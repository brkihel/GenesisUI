using System.Collections.Generic;
using GenesisUI.Foundation;
using GenesisUI.Foundation.Contracts;
using GenesisUI.Host;
using HarmonyLib;
using UnityEngine;

namespace GenesisUI.Modules.Minimap
{
    /// <summary>
    /// GenesisUI's own map markers (Diego, after R-058: dungeon, ore, monster spawn, base...). Vanilla
    /// has five fixed marker types and saves pins by type, so a new type could corrupt a save. A custom
    /// marker is a vanilla "point" pin whose name starts with a tag ("[gui:ore] Copper hill"): vanilla
    /// saves, shares and deletes it as always. This module gives tagged pins their icon and hides the
    /// tag in their label, on the large map and the minimap. Without GenesisUI the pin stays a point
    /// with the tag in its name.
    /// </summary>
    [GameContract("assembly_valheim", "Minimap", "get_instance")]
    [GameContract("assembly_valheim", "Minimap", "m_pins")]
    [GameContract("assembly_valheim", "Minimap+PinData", "m_name")]
    [GameContract("assembly_valheim", "Minimap+PinData", "m_icon")]
    [GameContract("assembly_valheim", "Minimap+PinData", "m_iconElement")]
    [GameContract("assembly_valheim", "Minimap+PinData", "m_NamePinData")]
    [GameContract("assembly_valheim", "Minimap+PinNameData", "get_PinNameText")]
    [GameContract("assembly_valheim", "ObjectDB", "get_instance")]
    [GameContract("assembly_valheim", "ObjectDB", "GetItemPrefab", Parameters = new[] { "System.String" })]
    [GameContract("assembly_valheim", "ZNetScene", "get_instance")]
    [GameContract("assembly_valheim", "ZNetScene", "GetPrefab", Parameters = new[] { "System.String" })]
    internal sealed class MapMarkersModule : IUiModule
    {
        internal const string TagPrefix = "[gui:";

        /// <summary>A custom marker kind: its tag id, its name token and the game object whose icon it borrows.</summary>
        internal sealed class Kind
        {
            public string Id, Token, Prefab;
            public Sprite Icon;
            public string Tag => TagPrefix + Id + "]";
        }

        internal static readonly Kind[] Kinds =
        {
            new Kind { Id = "dungeon", Token = "$genesisui_marker_dungeon", Prefab = "CryptKey" },
            new Kind { Id = "ore", Token = "$genesisui_marker_ore", Prefab = "CopperOre" },
            new Kind { Id = "spawn", Token = "$genesisui_marker_spawn", Prefab = "TrophyGreydwarf" },
            new Kind { Id = "base", Token = "$genesisui_marker_base", Prefab = "piece_workbench" },
            new Kind { Id = "portal", Token = "$genesisui_marker_portal", Prefab = "portal_wood" },
            new Kind { Id = "treasure", Token = "$genesisui_marker_treasure", Prefab = "Coins" },
            new Kind { Id = "trader", Token = "$genesisui_marker_trader", Prefab = "Amber" },
            new Kind { Id = "danger", Token = "$genesisui_marker_danger", Prefab = "TrophySkeleton" },
        };

        private static readonly string[] NoRegions = new string[0];
        private AccessTools.FieldRef<global::Minimap, List<global::Minimap.PinData>> _pins;
        private bool _iconsResolved;

        internal static bool Active { get; private set; }

        public string Id => "hud.mapmarkers";
        public string NameToken => "$genesisui_module_map_markers";
        public IReadOnlyList<string> Regions => NoRegions;
        public float RefreshRate => 0f;

        public void Build(ModuleContext context)
        {
            _pins = AccessTools.FieldRefAccess<global::Minimap, List<global::Minimap.PinData>>("m_pins");
            _iconsResolved = false;
            Active = true;
        }

        public void Teardown()
        {
            Active = false;
        }

        /// <summary>The icons come from the game's items and pieces, found once the databases exist.</summary>
        internal static void ResolveIcons()
        {
            foreach (var k in Kinds)
            {
                if (k.Icon != null) continue;
                GameObject prefab = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(k.Prefab) : null;
                if (prefab == null && ZNetScene.instance != null) prefab = ZNetScene.instance.GetPrefab(k.Prefab);
                if (prefab == null) continue;
                var drop = prefab.GetComponent<ItemDrop>();
                if (drop != null) k.Icon = drop.m_itemData.GetIcon();
                else
                {
                    var piece = prefab.GetComponent<Piece>();
                    if (piece != null) k.Icon = piece.m_icon;
                }
            }
        }

        internal static Kind KindOf(string name)
        {
            if (string.IsNullOrEmpty(name) || !name.StartsWith(TagPrefix, System.StringComparison.Ordinal)) return null;
            foreach (var k in Kinds) if (name.StartsWith(k.Tag, System.StringComparison.Ordinal)) return k;
            return null;
        }

        /// <summary>The name without its tag (what the label shows).</summary>
        internal static string Untagged(string name, Kind kind) => name.Substring(kind.Tag.Length).TrimStart();

        public void Refresh(float deltaSeconds)
        {
            var map = global::Minimap.instance;
            if (map == null) return;
            if (!_iconsResolved)
            {
                ResolveIcons();
                _iconsResolved = ObjectDB.instance != null && ZNetScene.instance != null;
            }
            var pins = _pins(map);
            if (pins == null) return;
            for (int i = 0; i < pins.Count; i++)
            {
                var pin = pins[i];
                var kind = pin != null ? KindOf(pin.m_name) : null;
                if (kind == null || kind.Icon == null) continue;
                // Vanilla builds pin elements from m_icon (and rebuilds them often): set both.
                if (pin.m_icon != kind.Icon) pin.m_icon = kind.Icon;
                if (pin.m_iconElement != null && pin.m_iconElement.sprite != kind.Icon) pin.m_iconElement.sprite = kind.Icon;
                var label = pin.m_NamePinData != null ? pin.m_NamePinData.PinNameText : null;
                if (label != null && label.text != null && label.text.StartsWith(TagPrefix, System.StringComparison.Ordinal))
                    label.text = Untagged(label.text, kind);
            }
        }
    }
}
