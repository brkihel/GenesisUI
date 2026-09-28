using System.Collections.Generic;
using BepInEx.Configuration;
using GenesisUI.Foundation;
using GenesisUI.Foundation.Contracts;
using UnityEngine;

namespace GenesisUI.Host
{
    internal sealed class ModuleEntry
    {
        public IUiModule Module;
        public ConfigEntry<bool> Enabled;
        public System.Action<float> RefreshAction;
        public ModuleState State = ModuleState.Disabled;
        public string Reason;
        public float SinceRefresh;
        public readonly List<VeilHandle> Veils = new List<VeilHandle>();
        public double RefreshSecondsTotal;
        public int RefreshCount;

        public string Owner => "module:" + Module.Id;
    }

    /// <summary>
    /// Builds, refreshes and tears down modules (docs/ARCHITECTURE.md §3). Guarantees,
    /// for every module: contracts checked before Build; every call guarded; a fault
    /// tears it down, lifts its veils and releases its regions and input; scene changes
    /// handled here, never by modules.
    /// </summary>
    [GameContract("assembly_valheim", "Minimap", "instance")]
    [GameContract("assembly_valheim", "Minimap", "m_largeRoot")]
    internal static class ModuleHost
    {
        /// <summary>The longest time step a module is ever given (seconds).</summary>
        private const float MaxRefreshDelta = 0.25f;

        private static readonly List<ModuleEntry> Entries = new List<ModuleEntry>();
        private static RectTransform _hudRoot;
        private static Theme.ThemeRuntime _theme;
        private static bool _masterEnabled = true;

        public static IReadOnlyList<ModuleEntry> Modules => Entries;

        public static void Init(Theme.ThemeRuntime theme, bool masterEnabled)
        {
            _theme = theme;
            _masterEnabled = masterEnabled;
            Guard.Faults.Tripped += record =>
            {
                foreach (var e in Entries)
                    if (e.Owner == record.Owner && e.State == ModuleState.Active) TearDown(e, ModuleState.Faulted, record.FirstMessage);
            };
        }

        public static void Register(IUiModule module, ConfigEntry<bool> enabled)
        {
            var entry = new ModuleEntry { Module = module, Enabled = enabled, RefreshAction = module.Refresh };
            Entries.Add(entry);
            enabled.SettingChanged += (_, __) => Guard.Try("toggle " + module.Id, () => Reconcile(entry));
            GenesisLog.Info("Host", "module registered: " + module.Id);
        }

        public static void SetMasterEnabled(bool enabled)
        {
            _masterEnabled = enabled;
            GenesisLog.Info("Host", "GenesisUI " + (enabled ? "enabled" : "disabled") + " by config");
            foreach (var e in Entries) Reconcile(e);
        }

        /// <summary>Jötunn raised OnCustomGUIAvailable: the main scene may now have a HUD.</summary>
        public static void OnGuiAvailable()
        {
            if (Hud.instance == null || Hud.instance.m_rootObject == null) return; // main menu
            EnsureHudRoot();
            KeepBelowLargeMap();
            foreach (var e in Entries) Reconcile(e);
        }

        /// <summary>Plugin.Update.</summary>
        public static void Tick(float dt)
        {
            if (_hudRoot == null && HasBuiltModules()) OnSceneLost();

            foreach (var e in Entries)
            {
                if (e.State != ModuleState.Active) continue;
                e.SinceRefresh += dt;
                float period = e.Module.RefreshRate > 0f ? 1f / e.Module.RefreshRate : 0f;
                if (e.SinceRefresh < period) continue;

                // Never hand a module a nonsense delta: a fresh build asks for a refresh with
                // SinceRefresh = MaxValue, and animation clocks fed that turned NaN (striped
                // vital bars after a fault + retry, R-040).
                float elapsed = Mathf.Min(e.SinceRefresh, MaxRefreshDelta);
                e.SinceRefresh = 0f;
                long start = System.Diagnostics.Stopwatch.GetTimestamp();
                Guard.Run(e.Owner, e.RefreshAction, elapsed);
                e.RefreshSecondsTotal += (System.Diagnostics.Stopwatch.GetTimestamp() - start) / (double)System.Diagnostics.Stopwatch.Frequency;
                e.RefreshCount++;
            }
        }

        /// <summary>Plugin.LateUpdate.</summary>
        public static void LateTick()
        {
            VanillaVeil.Enforce();
            VanillaNudge.Enforce();
        }

#if GENESIS_DIAGNOSTICS
        private static readonly System.Action<string> ThrowInjected = id =>
            throw new System.InvalidOperationException("Fault injected from the diagnostics panel into " + id);

        /// <summary>Diagnostics: make the module fail through the same guard a real bug would.</summary>
        public static void InjectFault(ModuleEntry e)
        {
            if (e.State != ModuleState.Active) return;
            GenesisLog.Warn("Host", "injecting a fault into " + e.Module.Id + " (diagnostics)");
            Guard.Run(e.Owner, ThrowInjected, e.Module.Id);
        }
#endif

        /// <summary>Diagnostics "retry": forget the fault and try to build again.</summary>
        public static void Retry(ModuleEntry e)
        {
            Guard.Faults.Reset(e.Owner);
            e.State = ModuleState.Disabled;
            e.Reason = null;
            Reconcile(e);
        }

        private static bool HasBuiltModules()
        {
            foreach (var e in Entries) if (e.State == ModuleState.Active) return true;
            return false;
        }

        private static void OnSceneLost()
        {
            // The HUD and everything under it went away with the scene.
            foreach (var e in Entries)
                if (e.State == ModuleState.Active) TearDown(e, ModuleState.Waiting, null);
            GenesisLog.Info("Host", "HUD gone (scene change); modules wait for the next one");
        }

        private static void Reconcile(ModuleEntry e)
        {
            bool wanted = _masterEnabled && e.Enabled.Value;

            if (!wanted)
            {
                if (e.State == ModuleState.Active) TearDown(e, ModuleState.Disabled, null);
                else if (e.State != ModuleState.Faulted) Set(e, ModuleState.Disabled, null);
                return;
            }

            if (e.State == ModuleState.Active || e.State == ModuleState.Faulted) return;
            if (_hudRoot == null) { Set(e, ModuleState.Waiting, null); return; }
            Build(e);
        }

        private static void Build(ModuleEntry e)
        {
            var missing = ContractResolver.Missing(e.Module.GetType());
            if (missing.Count > 0) { Set(e, ModuleState.Unsupported, "missing " + string.Join("; ", missing)); return; }

            if (!RegionRegistry.TryClaimAll(e.Owner, e.Module.Regions, out string reason)) { Set(e, ModuleState.Blocked, reason); return; }

            var root = new GameObject("GenesisUI_" + e.Module.Id, typeof(RectTransform));
            var rt = (RectTransform)root.transform;
            rt.SetParent(_hudRoot, false);
            Stretch(rt);

            bool built = Guard.Run(e.Owner, () => e.Module.Build(new ModuleContext { Root = rt, Theme = _theme }));
            if (!built)
            {
                Object.Destroy(root);
                RegionRegistry.ReleaseAll(e.Owner);
                if (e.State != ModuleState.Faulted) Set(e, ModuleState.Faulted, "Build threw; see the log");
                return;
            }

            foreach (var region in e.Module.Regions)
            {
                int veiled = 0;
                foreach (var target in RegionRegistry.Resolve(region))
                {
                    var handle = VanillaVeil.Apply(e.Owner, region + "/" + target.name, target);
                    if (handle != null) { e.Veils.Add(handle); veiled++; }
                }
                if (veiled == 0 && !RegionRegistry.IsDynamic(region))
                    GenesisLog.Warn("Veil", e.Module.Id + ": no vanilla object found for " + region);
            }
            e.SinceRefresh = float.MaxValue; // refresh on the next tick
            Set(e, ModuleState.Active, null);
        }

        private static void TearDown(ModuleEntry e, ModuleState next, string reason)
        {
            Guard.Try("teardown " + e.Module.Id, e.Module.Teardown);
            VanillaVeil.RestoreAll(e.Owner);
            VanillaNudge.RestoreAll(e.Owner);
            e.Veils.Clear();
            InputLeases.ReleaseAll(e.Owner);
            RegionRegistry.ReleaseAll(e.Owner);
            Set(e, next, reason);
        }

        private static void Set(ModuleEntry e, ModuleState state, string reason)
        {
            if (e.State == state && e.Reason == reason) return;
            GenesisLog.Info("Host", e.Module.Id + ": " + e.State + " -> " + state + (reason == null ? "" : " (" + reason + ")"));
            e.State = state;
            e.Reason = reason;
        }

        private static void EnsureHudRoot()
        {
            if (_hudRoot != null) return;
            var go = new GameObject("GenesisUI_Hud", typeof(RectTransform));
            _hudRoot = (RectTransform)go.transform;
            // Under the vanilla HUD root: GenesisUI hides with the HUD (Ctrl+F3, death,
            // cutscenes) and inherits the game's GUI scale, with no extra logic.
            // Vanilla hides the HUD by moving m_rootObject to x=10000 (Hud.SetVisible), so
            // children follow it for free.
            var parent = Hud.instance.m_rootObject.transform as RectTransform;
            _hudRoot.SetParent(Hud.instance.m_rootObject.transform, false);
            _hudRoot.SetAsLastSibling();
            Stretch(_hudRoot);
            FitToCanvas(parent);
        }

        /// <summary>
        /// Our root was the last sibling under the vanilla HUD root, and the interaction card drew
        /// over the open large map (Vegvísir, R-040). When the large map hangs under the same
        /// root, put ours just before the map's branch so, like vanilla's HUD, it stays under the
        /// map. The log line says which case the game is in.
        /// </summary>
        private static void KeepBelowLargeMap()
        {
            if (_hudRoot == null || Minimap.instance == null || Minimap.instance.m_largeRoot == null) return;
            var parent = _hudRoot.parent;
            var branch = Minimap.instance.m_largeRoot.transform;
            while (branch != null && branch.parent != parent) branch = branch.parent;
            if (branch == null)
            {
                GenesisLog.Info("Host", "large map is outside the HUD root; no reordering needed");
                return;
            }
            int mapIndex = branch.GetSiblingIndex();
            int ours = _hudRoot.GetSiblingIndex();
            if (ours < mapIndex) return;
            _hudRoot.SetSiblingIndex(mapIndex);
            GenesisLog.Info("Host", "HUD root placed below the large map ('" + branch.name + "')");
        }

        /// <summary>
        /// Layout assumes the HUD root covers the whole canvas. If the vanilla root is
        /// smaller, keep our root canvas-sized and centred on it, and log the numbers so a
        /// test report shows which case the game is in.
        /// </summary>
        private static void FitToCanvas(RectTransform parent)
        {
            var canvas = parent != null ? parent.GetComponentInParent<Canvas>() : null;
            var canvasRect = canvas != null ? canvas.rootCanvas.transform as RectTransform : null;
            if (parent == null || canvasRect == null)
            {
                GenesisLog.Warn("Host", "HUD root: no canvas found above m_rootObject; stretching to the parent");
                return;
            }

            Vector2 parentSize = parent.rect.size, canvasSize = canvasRect.rect.size;
            bool fullScreen = Mathf.Abs(parentSize.x - canvasSize.x) < 2f && Mathf.Abs(parentSize.y - canvasSize.y) < 2f;
            GenesisLog.Info("Host", "HUD root under " + parent.name + ": parent " + parentSize + ", canvas " + canvasSize
                                    + " (" + canvas.rootCanvas.name + ", scale " + canvas.rootCanvas.scaleFactor.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + ")"
                                    + (fullScreen ? ", stretched" : ", parent is smaller: using a canvas-sized root"));
            if (fullScreen) return;

            _hudRoot.anchorMin = _hudRoot.anchorMax = _hudRoot.pivot = new Vector2(0.5f, 0.5f);
            _hudRoot.sizeDelta = canvasSize;
            _hudRoot.anchoredPosition = -parent.rect.center;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
