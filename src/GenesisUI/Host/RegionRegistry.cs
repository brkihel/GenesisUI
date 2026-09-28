using System;
using System.Collections.Generic;
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
    [GameContract("assembly_valheim", "Hud", "m_healthBarRoot")]
    [GameContract("assembly_valheim", "Hud", "m_staminaBar2Root")]
    [GameContract("assembly_valheim", "Hud", "m_eitrBarRoot")]
    internal static class RegionRegistry
    {
        private static readonly Dictionary<string, Func<GameObject>> Resolvers = new Dictionary<string, Func<GameObject>>(StringComparer.Ordinal)
        {
            ["hud.health"] = () => Hud.instance != null && Hud.instance.m_healthBarRoot != null ? Hud.instance.m_healthBarRoot.gameObject : null,
            ["hud.stamina"] = () => Hud.instance != null && Hud.instance.m_staminaBar2Root != null ? Hud.instance.m_staminaBar2Root.gameObject : null,
            ["hud.eitr"] = () => Hud.instance != null && Hud.instance.m_eitrBarRoot != null ? Hud.instance.m_eitrBarRoot.gameObject : null,
        };

        /// <summary>Mods that redraw vanilla regions: GUID → regions they own while installed.</summary>
        private static readonly Dictionary<string, string[]> ForeignOwners = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            // SeneaL UI replaces the whole HUD. While both are installed, it keeps it.
            ["seneaL.valheim.ui"] = new[] { "hud.health", "hud.stamina", "hud.eitr" },
        };

        private static readonly Dictionary<string, string> Owners = new Dictionary<string, string>(StringComparer.Ordinal);

        public static bool IsKnown(string region) => Resolvers.ContainsKey(region);

        public static GameObject Resolve(string region) => Resolvers.TryGetValue(region, out var r) ? r() : null;

        /// <summary>Claims every region or none.</summary>
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
