using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Bootstrap;
using GenesisUI.Foundation.Contracts;
using UnityEngine;

namespace GenesisUI.Host
{
    /// <summary>
    /// Named pieces of the vanilla UI and who owns each one (docs/ARCHITECTURE.md §3,
    /// docs/regions.md). One owner per region. A known mod that redraws a region blocks
    /// it: GenesisUI and that mod never fight over the same pixels.
    /// </summary>
    [GameContract("assembly_valheim", "Hud", "instance")]
    [GameContract("assembly_valheim", "Hud", "m_rootObject")]
    [GameContract("assembly_valheim", "Hud", "m_healthBarRoot")]
    [GameContract("assembly_valheim", "Hud", "m_staminaBar2Root")]
    [GameContract("assembly_valheim", "Hud", "m_eitrBarRoot")]
    [GameContract("assembly_valheim", "Hud", "m_foodBarRoot")]
    [GameContract("assembly_valheim", "Hud", "m_foodBaseBar")]
    [GameContract("assembly_valheim", "Hud", "m_foodIcon")]
    [GameContract("assembly_valheim", "Hud", "m_foodText")]
    [GameContract("assembly_valheim", "Hud", "m_foodIcons")]
    [GameContract("assembly_valheim", "Hud", "m_foodTime")]
    [GameContract("assembly_valheim", "Hud", "m_foodBars")]
    [GameContract("assembly_valheim", "Hud", "m_statusEffectListRoot")]
    [GameContract("assembly_valheim", "Hud", "m_gpRoot")]
    [GameContract("assembly_valheim", "Hud", "m_healthPanel")]
    [GameContract("assembly_valheim", "Minimap", "instance")]
    [GameContract("assembly_valheim", "Minimap", "m_smallRoot")]
    [GameContract("assembly_valheim", "Hud", "m_hoverName")]
    [GameContract("assembly_valheim", "MessageHud", "instance")]
    [GameContract("assembly_valheim", "MessageHud", "m_messageText")]
    [GameContract("assembly_valheim", "MessageHud", "m_messageIcon")]
    [GameContract("assembly_valheim", "MessageHud", "m_messageCenterText")]
    [GameContract("assembly_valheim", "EnemyHud", "instance")]
    [GameContract("assembly_valheim", "EnemyHud", "m_hudRoot")]
    [GameContract("assembly_valheim", "EnemyHud", "m_baseHudBoss")]
    internal static class RegionRegistry
    {
        private static readonly Dictionary<string, Func<IEnumerable<GameObject>>> Resolvers = new Dictionary<string, Func<IEnumerable<GameObject>>>(StringComparer.Ordinal)
        {
            ["hud.health"] = () => FromHud(h => h.m_healthBarRoot),
            ["hud.stamina"] = () => FromHud(h => h.m_staminaBar2Root),
            ["hud.eitr"] = () => FromHud(h => h.m_eitrBarRoot),
            ["hud.food"] = Food,
            // What is left in the vanilla health panel once bars and food have owners: its
            // decoration (R-020 showed a red emblem and a gold tick still visible).
            ["hud.healthDecor"] = HealthDecor,
            ["hud.statusEffects"] = () => FromHud(h => h.m_statusEffectListRoot),
            ["hud.guardianPower"] = () => FromHud(h => h.m_gpRoot),
            ["hud.hover"] = () => FromHud(h => h.m_hoverName),
            ["hud.messages"] = Messages,
            // Boss HUDs are created and destroyed by vanilla while playing; the boss module veils
            // new ones as they appear (docs/regions.md).
            ["hud.boss"] = BossHuds,
            ["hud.minimap"] = () => Minimap.instance != null && Minimap.instance.m_smallRoot != null
                ? new[] { Minimap.instance.m_smallRoot } : Enumerable.Empty<GameObject>(),
            // HotkeyBar is its own component under the HUD; its Update keeps gamepad
            // selection and use working while veiled (see docs/regions.md).
            ["hud.hotbar"] = () => Hud.instance == null ? Enumerable.Empty<GameObject>()
                : Hud.instance.GetComponentsInChildren<HotkeyBar>(true).Select(b => b.gameObject),
        };

        /// <summary>Mods that redraw vanilla regions: GUID → regions they own while installed.</summary>
        private static readonly Dictionary<string, string[]> ForeignOwners = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            // SeneaL UI replaces the whole HUD. While both are installed, it keeps it.
            ["seneaL.valheim.ui"] = new[] { "hud.health", "hud.stamina", "hud.eitr", "hud.healthDecor", "hud.food", "hud.statusEffects", "hud.guardianPower", "hud.hotbar", "hud.minimap", "hud.hover", "hud.messages", "hud.boss" },
        };

        /// <summary>Regions whose vanilla objects come and go while playing: empty is normal for them.</summary>
        private static readonly HashSet<string> Dynamic = new HashSet<string>(StringComparer.Ordinal) { "hud.boss" };

        public static bool IsDynamic(string region) => Dynamic.Contains(region);

        private static IEnumerable<GameObject> Messages()
        {
            var m = MessageHud.instance;
            if (m == null) yield break;
            if (m.m_messageText != null) yield return m.m_messageText.gameObject;
            if (m.m_messageIcon != null) yield return m.m_messageIcon.gameObject;
            if (m.m_messageCenterText != null) yield return m.m_messageCenterText.gameObject;
        }

        /// <summary>Live vanilla boss HUDs: clones of m_baseHudBoss under m_hudRoot.</summary>
        public static IEnumerable<GameObject> BossHuds()
        {
            var e = EnemyHud.instance;
            if (e == null || e.m_hudRoot == null || e.m_baseHudBoss == null) yield break;
            string prefix = e.m_baseHudBoss.name;
            var root = e.m_hudRoot.transform;
            for (int i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                if (child.name.StartsWith(prefix, StringComparison.Ordinal)) yield return child.gameObject;
            }
        }

        private static readonly Dictionary<string, string> Owners = new Dictionary<string, string>(StringComparer.Ordinal);

        public static bool IsKnown(string region) => Resolvers.ContainsKey(region);

        /// <summary>Every live vanilla object of the region (may be empty).</summary>
        public static IEnumerable<GameObject> Resolve(string region) =>
            Resolvers.TryGetValue(region, out var r) ? r().Where(go => go != null) : Enumerable.Empty<GameObject>();

        /// <summary>Uses Unity's null check (a destroyed HUD compares equal to null; C#'s ?. would not).</summary>
        private static IEnumerable<GameObject> FromHud(Func<Hud, Component> pick)
        {
            var hud = Hud.instance;
            if (hud == null) yield break;
            var c = pick(hud);
            if (c != null) yield return c.gameObject;
        }

        /// <summary>The food strip is several loose objects: bars, icons and times per food, plus the hunger icon.</summary>
        private static IEnumerable<GameObject> Food()
        {
            var hud = Hud.instance;
            if (hud == null) yield break;
            if (hud.m_foodBarRoot != null) yield return hud.m_foodBarRoot.gameObject;
            if (hud.m_foodBaseBar != null) yield return hud.m_foodBaseBar.gameObject;
            if (hud.m_foodIcon != null) yield return hud.m_foodIcon.gameObject;
            if (hud.m_foodText != null) yield return hud.m_foodText.gameObject;
            foreach (var c in (Component[])hud.m_foodIcons ?? Array.Empty<Component>())
            {
                if (c == null) continue;
                yield return c.gameObject;
                // The empty slot frame each icon sits in (R-020: three empty frames showed under our bars).
                var frame = c.transform.parent;
                if (frame != null && !IsStructural(hud, frame)) yield return frame.gameObject;
            }
            foreach (var c in (Component[])hud.m_foodTime ?? Array.Empty<Component>()) if (c != null) yield return c.gameObject;
            foreach (var c in (Component[])hud.m_foodBars ?? Array.Empty<Component>()) if (c != null) yield return c.gameObject;
        }

        /// <summary>Claims every region or none.</summary>
        private static bool IsStructural(Hud hud, Transform t) =>
            t == hud.transform || t == hud.m_rootObject.transform
            || (hud.m_healthPanel != null && t == hud.m_healthPanel.transform)
            || (hud.m_foodBarRoot != null && t == hud.m_foodBarRoot.transform);

        /// <summary>
        /// Direct children of the vanilla health panel that hold nothing another region owns.
        /// A child containing a food piece or a bar root is left alone: its owner veils it.
        /// </summary>
        private static IEnumerable<GameObject> HealthDecor()
        {
            var hud = Hud.instance;
            if (hud == null || hud.m_healthPanel == null) yield break;

            var owned = new List<Transform>();
            foreach (var go in Food()) owned.Add(go.transform);
            foreach (var c in new Component[] { hud.m_healthBarRoot, hud.m_staminaBar2Root, hud.m_eitrBarRoot })
                if (c != null) owned.Add(c.transform);

            foreach (Transform child in hud.m_healthPanel)
            {
                bool holdsOwned = false;
                foreach (var o in owned)
                    if (o == child || o.IsChildOf(child)) { holdsOwned = true; break; }
                if (!holdsOwned) yield return child.gameObject;
            }
        }

        public static bool TryClaimAll(string owner, IReadOnlyList<string> regions, out string reason)
        {
            foreach (var region in regions)
            {
                if (!IsKnown(region)) { reason = "unknown region " + region; return false; }
                if (Owners.TryGetValue(region, out var current) && current != owner) { reason = region + " is owned by " + current; return false; }
                foreach (var foreign in ForeignOwners)
                {
                    if (Array.IndexOf(foreign.Value, region) >= 0 && Chainloader.PluginInfos.ContainsKey(foreign.Key))
                    {
                        reason = region + " is drawn by " + Chainloader.PluginInfos[foreign.Key].Metadata.Name + " (" + foreign.Key + ")";
                        return false;
                    }
                }
            }
            foreach (var region in regions) Owners[region] = owner;
            reason = null;
            return true;
        }

        public static void ReleaseAll(string owner)
        {
            var mine = new List<string>();
            foreach (var kv in Owners) if (kv.Value == owner) mine.Add(kv.Key);
            foreach (var r in mine) Owners.Remove(r);
        }

        public static IEnumerable<KeyValuePair<string, string>> Snapshot()
        {
            foreach (var region in Resolvers.Keys)
                yield return new KeyValuePair<string, string>(region, Owners.TryGetValue(region, out var o) ? o : "vanilla");
        }
    }
}
