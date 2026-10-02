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
        public RectTransform Root;
        public int Generation;
        public System.EventHandler ConfigChanged;

        public string Owner => "module:" + Module.Id;
    }

    /// <summary>
    /// Builds, refreshes and tears down modules (docs/ARCHITECTURE.md §3). Guarantees,
    /// for every module: contracts checked before Build; every call guarded; a fault
    /// tears it down, lifts its veils and releases its regions and input; scene changes
    /// handled here, never by modules.
    /// </summary>
    [GameContract("assembly_valheim", "Minimap", "instance")]
    [GameContract("assembly_valheim", "Minimap", "m_largeRoot", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "UnityEngine.GameObject")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "Hud", "get_instance", Parameters = new string[] {  }, Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Method, Static = GenesisUI.Foundation.Contracts.ContractStatic.Static, ValueType = "Hud")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "Hud", "m_rootObject", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "UnityEngine.GameObject")]
    internal static class ModuleHost
    {
        /// <summary>The longest time step a module is ever given (seconds).</summary>
        private const float MaxRefreshDelta = 0.25f;

        private static readonly List<ModuleEntry> Entries = new List<ModuleEntry>();
        private static RectTransform _hudRoot;
        private static Theme.ThemeRuntime _theme;
        private static bool _masterEnabled = true;
        private static readonly Foundation.Faults.RecoveryQueue Recovery = new Foundation.Faults.RecoveryQueue();
        private static readonly System.Action<Foundation.Faults.FaultRecord> RecoverPending = RecoverOwner;

        public static IReadOnlyList<ModuleEntry> Modules => Entries;

        public static void Init(Theme.ThemeRuntime theme, bool masterEnabled)
        {
            Guard.Faults.Tripped -= OnFault;
            _theme = theme;
            _masterEnabled = masterEnabled;
            Guard.Faults.Tripped += OnFault;
        }

        private static void OnFault(Foundation.Faults.FaultRecord record)
        {
            foreach (var entry in Entries)
                if (entry.Owner == record.Owner && (entry.State == ModuleState.Active || entry.State == ModuleState.Building))
                    Recovery.Request(record);
        }

        private static void RecoverOwner(Foundation.Faults.FaultRecord record)
        {
            foreach (var entry in Entries)
                if (entry.Owner == record.Owner)
                    Guard.Try("recover " + entry.Module.Id, () => Recover(entry, record));
        }

        public static void Register(IUiModule module, ConfigEntry<bool> enabled)
        {
            var entry = new ModuleEntry { Module = module, Enabled = enabled, RefreshAction = module.Refresh };
            Entries.Add(entry);
            entry.ConfigChanged = (_, __) => Guard.Try("toggle " + module.Id, ReconcileAll);
            enabled.SettingChanged += entry.ConfigChanged;
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

        /// <summary>Plugin.LateUpdate, before <see cref="LateTick"/>.</summary>
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
            ReconcileDependencies();
            Recovery.Drain(RecoverPending); // fault notifications have returned; restore before drawing
            ReconcileDependencies();
        }

        /// <summary>Plugin.LateUpdate.</summary>
        public static void LateTick()
        {
            VanillaVeil.Enforce();
            VanillaNudge.Enforce();
            Guard.Try("fault popup tick", Diagnostics.FaultPopup.Tick);
        }

#if GENESIS_DIAGNOSTICS
        private static readonly System.Action<string> ThrowInjected = id =>
            throw new System.InvalidOperationException("Fault injected from the diagnostics panel into " + id);
#endif

        /// <summary>
        /// Opacity of every HUD module at once (0-1), without touching the modules: the window shell
        /// fades the HUD out while a window is open (Diego, R-046) and back in when it closes.
        /// </summary>
        public static void SetHudAlpha(float alpha)
        {
            if (_hudRoot == null) return;
            var group = _hudRoot.GetComponent<CanvasGroup>();
            if (group == null)
            {
                if (alpha >= 1f) return;
                group = _hudRoot.gameObject.AddComponent<CanvasGroup>();
                group.interactable = false;
                group.blocksRaycasts = false;
            }
            if (!Mathf.Approximately(group.alpha, alpha)) group.alpha = alpha;
        }

#if GENESIS_DIAGNOSTICS
        /// <summary>Diagnostics: make the module fail through the same guard a real bug would.</summary>
        public static void InjectFault(ModuleEntry e)
        {
            if (e.State != ModuleState.Active) return;
            GenesisLog.Warn("Host", "injecting a fault into " + e.Module.Id + " (diagnostics)");
            Guard.Run(e.Owner, ThrowInjected, e.Module.Id);
        }
#endif

        private static readonly Foundation.Faults.RecoveryPolicy Policy = new Foundation.Faults.RecoveryPolicy();

        /// <summary>
        /// A module threw (Diego, after R-058: a broken window must never stay broken, vanilla must
        /// not show up, the player must not have to press "re-enable"). The vanilla window it drew over
        /// is closed first; after cleanup subscribers return, the module is torn down and rebuilt, so HUD veils are
        /// back before anything draws; the player is told with an apology and the exact error. A module
        /// that keeps failing (RecoveryPolicy) stays off and the popup says so.
        /// </summary>
        private static void Recover(ModuleEntry e, Foundation.Faults.FaultRecord record)
        {
            Set(e, ModuleState.Recovering, record.FirstMessage);
            if (e.Module is IRecoverable window) Guard.Try("close vanilla window for " + e.Module.Id, window.CloseVanillaWindow);
            TearDown(e, ModuleState.Faulted, record.FirstMessage);
            bool restart = _masterEnabled && e.Enabled.Value && Policy.TryRestart(e.Owner, Guard.Now);
            GenesisLog.Warn("Host", e.Module.Id + (restart ? " faulted; rebuilding it now" : " faulted again; left off (too many restarts)"));
            Guard.Try("fault popup", () => Diagnostics.FaultPopup.Show(e.Module.NameToken, record, restart));
            if (!restart) return;
            Guard.Faults.Reset(e.Owner);
            e.State = ModuleState.Disabled;
            e.Reason = null;
            Reconcile(e);
        }

        /// <summary>Diagnostics "retry": forget the fault and try to build again.</summary>
        public static void Retry(ModuleEntry e)
        {
            Recovery.Forget(e.Owner);
            TearDown(e, ModuleState.Disabled, null);
            Policy.Forget(e.Owner);
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
            {
                Recovery.Forget(e.Owner);
                if (e.State == ModuleState.Active) TearDown(e, ModuleState.Waiting, null);
            }
            GenesisLog.Info("Host", "HUD gone (scene change); modules wait for the next one");
        }

        private static bool NeedsShell(ModuleEntry entry) => entry.Module.Id == "win.inventory" || entry.Module.Id == "win.crafting" || entry.Module.Id == "win.skills" || entry.Module.Id == "win.achievements" || entry.Module.Id == "win.settings";
        internal static bool IsActive(string id)
        {
            foreach (var entry in Entries) if (entry.Module.Id == id) return entry.State == ModuleState.Active && !Guard.IsTripped(entry.Owner);
            return false;
        }
        private static void ReconcileAll() { foreach (var entry in Entries) Reconcile(entry); }
        private static void ReconcileDependencies()
        {
            bool shell = IsActive("win.shell");
            foreach (var entry in Entries)
            {
                if (!NeedsShell(entry)) continue;
                if (!shell && entry.State == ModuleState.Active) TearDown(entry, ModuleState.Blocked, "window shell unavailable");
                else if (shell && entry.State == ModuleState.Blocked && entry.Reason == "window shell unavailable") Reconcile(entry);
            }
        }

        private static void Reconcile(ModuleEntry e)
        {
            bool wanted = _masterEnabled && e.Enabled.Value;

            if (!wanted)
            {
                Recovery.Forget(e.Owner);
                if (e.State == ModuleState.Active) TearDown(e, ModuleState.Disabled, null);
                else if (e.State != ModuleState.Faulted) Set(e, ModuleState.Disabled, null);
                return;
            }

            if (e.State == ModuleState.Active || e.State == ModuleState.Faulted || e.State == ModuleState.Building || e.State == ModuleState.Recovering) return;
            if (_hudRoot == null) { Set(e, ModuleState.Waiting, null); return; }
            if (NeedsShell(e) && !IsActive("win.shell")) { Set(e, ModuleState.Blocked, "window shell unavailable"); return; }
            Build(e);
        }

        private static void Build(ModuleEntry e)
        {
            if (!Guard.Run(e.Owner, () => BuildChecked(e))) TearDown(e, ModuleState.Faulted, "Build preflight threw; see the log");
        }

        private static void BuildChecked(ModuleEntry e)
        {
            if (e.Module is IModulePrerequisites prerequisites && prerequisites.UnsupportedReason != null)
            {
                Set(e, ModuleState.Unsupported, prerequisites.UnsupportedReason);
                return;
            }
            var missing = ContractResolver.Missing(e.Module.GetType());
            if (missing.Count > 0) { Set(e, ModuleState.Unsupported, "missing " + string.Join("; ", missing)); return; }

            if (!RegionRegistry.TryClaimAll(e.Owner, e.Module.Regions, out string reason)) { Set(e, ModuleState.Blocked, reason); return; }

            Set(e, ModuleState.Building, null);
            e.Generation++;
            bool built = Guard.Run(e.Owner, () =>
            {
                e.Root = (RectTransform)OwnerResources.Own(new GameObject("GenesisUI_" + e.Module.Id, typeof(RectTransform))).transform;
                e.Root.SetParent(_hudRoot, false);
                Stretch(e.Root);
                e.Module.Build(new ModuleContext { Root = e.Root, Theme = _theme, Owner = e.Owner, Generation = e.Generation });
                VeilRegions(e);
            });
            if (!built)
            {
                TearDown(e, ModuleState.Faulted, "Build threw; see the log");
                return;
            }
            e.SinceRefresh = float.MaxValue;
            Set(e, ModuleState.Active, null);
        }

        private static void VeilRegions(ModuleEntry e)
        {
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
        }

        private static void TearDown(ModuleEntry e, ModuleState next, string reason)
        {
            Guard.Try("teardown " + e.Module.Id, e.Module.Teardown);
            Guard.Try(e.Owner + " restore skins", () => VanillaSkin.RestoreAll(e.Owner));
            Guard.Try(e.Owner + " release shared panels", () => GenesisUI.Modules.Windows.VanillaPanels.Release(e.Owner));
            Guard.Try(e.Owner + " restore veils", () => VanillaVeil.RestoreAll(e.Owner));
            Guard.Try(e.Owner + " restore nudges", () => VanillaNudge.RestoreAll(e.Owner));
            e.Veils.Clear();
            Guard.Try(e.Owner + " release input", () => InputLeases.ReleaseAll(e.Owner));
            Guard.Try(e.Owner + " release regions", () => RegionRegistry.ReleaseAll(e.Owner));
            OwnerResources.Release(e.Owner);
            e.Root = null;
            Set(e, next, reason);
        }

        public static void Shutdown()
        {
            Guard.Faults.Tripped -= OnFault;
            Recovery.Clear();
            foreach (var entry in Entries)
            {
                entry.Enabled.SettingChanged -= entry.ConfigChanged;
                TearDown(entry, ModuleState.Disabled, null);
                Policy.Forget(entry.Owner);
            }
            Entries.Clear();
            if (_hudRoot != null) Object.Destroy(_hudRoot.gameObject);
            _hudRoot = null;
            _theme = null;
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
