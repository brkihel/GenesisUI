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
    [GameContract("assembly_valheim", "Minimap", "m_pins", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Collections.Generic.List\u00601[[Minimap\u002BPinData, assembly_valheim, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]]")]
    [GameContract("assembly_valheim", "Minimap+PinData", "m_name", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.String")]
    [GameContract("assembly_valheim", "Minimap+PinData", "m_icon", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "UnityEngine.Sprite")]
    [GameContract("assembly_valheim", "Minimap+PinData", "m_iconElement", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "UnityEngine.UI.Image")]
    [GameContract("assembly_valheim", "Minimap+PinData", "m_NamePinData", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "Minimap\u002BPinNameData")]
    [GameContract("assembly_valheim", "Minimap+PinNameData", "get_PinNameText")]
    [GameContract("assembly_valheim", "ObjectDB", "get_instance")]
    [GameContract("assembly_valheim", "ObjectDB", "GetItemPrefab", Parameters = new[] { "System.String" })]
    [GameContract("assembly_valheim", "ZNetScene", "get_instance")]
    [GameContract("assembly_valheim", "ZNetScene", "GetPrefab", Parameters = new[] { "System.String" })]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "ItemDrop", "m_itemData", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "ItemDrop\u002BItemData")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "ItemDrop\u002BItemData", "GetIcon", Parameters = new string[] {  }, Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Method, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "UnityEngine.Sprite")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "Piece", "m_icon", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "UnityEngine.Sprite")]
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
        private global::Minimap _map;
        private readonly Dictionary<global::Minimap.PinData, Appearance> _appearances = new Dictionary<global::Minimap.PinData, Appearance>();
        private readonly List<global::Minimap.PinData> _removed = new List<global::Minimap.PinData>();
        private float _pruneIn;
        private sealed class Appearance
        {
            internal Sprite Original, Written, ElementOriginal;
            internal UnityEngine.UI.Image Element;
            internal TMPro.TMP_Text Label;
            internal string OriginalText, WrittenText;
            internal void Restore(global::Minimap.PinData pin)
            {
                if (pin.m_icon == Written) pin.m_icon = Original;
                if (Element != null && Element.sprite == Written) Element.sprite = ElementOriginal;
                if (Label != null && Label.text == WrittenText) Label.text = OriginalText;
            }
        }
        private void RestoreAppearances()
        {
            foreach (var entry in _appearances) entry.Value.Restore(entry.Key);
            _appearances.Clear(); _map = null;
        }

        internal static bool Active { get; private set; }

        public string Id => "hud.mapmarkers";
        public string NameToken => "$genesisui_module_map_markers";
        public IReadOnlyList<string> Regions => new[] { Id };
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
            RestoreAppearances();
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
            if (_map != map) { RestoreAppearances(); _map = map; }
            if (!_iconsResolved)
            {
                ResolveIcons();
                _iconsResolved = ObjectDB.instance != null && ZNetScene.instance != null;
            }
            var pins = _pins(map);
            if (pins == null) return;
            _pruneIn -= deltaSeconds;
            if (_pruneIn <= 0f)
            {
                _pruneIn = 5f; _removed.Clear();
                foreach (var entry in _appearances) if (!pins.Contains(entry.Key)) _removed.Add(entry.Key);
                foreach (var pin in _removed) { _appearances[pin].Restore(pin); _appearances.Remove(pin); }
            }
            for (int i = 0; i < pins.Count; i++)
            {
                var pin = pins[i];
                var kind = pin != null ? KindOf(pin.m_name) : null;
                if (pin == null) continue;
                if (kind == null || kind.Icon == null)
                {
                    if (_appearances.TryGetValue(pin, out var previous)) { previous.Restore(pin); _appearances.Remove(pin); }
                    continue;
                }
                if (!_appearances.TryGetValue(pin, out var state))
                { state = new Appearance { Original = pin.m_icon }; _appearances.Add(pin, state); }
                else if (pin.m_icon != state.Written) state.Original = pin.m_icon;
                if (state.Element != pin.m_iconElement)
                {
                    if (state.Element != null && state.Element.sprite == state.Written) state.Element.sprite = state.ElementOriginal;
                    state.Element = pin.m_iconElement;
                    state.ElementOriginal = state.Element != null && state.Element.sprite != state.Written ? state.Element.sprite : state.Original;
                }
                else if (state.Element != null && state.Element.sprite != state.Written) state.ElementOriginal = state.Element.sprite;
                state.Written = kind.Icon;
                // Vanilla builds pin elements from m_icon (and rebuilds them often): set both.
                if (pin.m_icon != kind.Icon) pin.m_icon = kind.Icon;
                if (pin.m_iconElement != null && pin.m_iconElement.sprite != kind.Icon) pin.m_iconElement.sprite = kind.Icon;
                var label = pin.m_NamePinData != null ? pin.m_NamePinData.PinNameText : null;
                if (state.Label != label)
                {
                    if (state.Label != null && state.Label.text == state.WrittenText) state.Label.text = state.OriginalText;
                    state.Label = label; state.OriginalText = label != null ? label.text : null; state.WrittenText = null;
                }
                if (label != null && label.text != null && label.text.StartsWith(TagPrefix, System.StringComparison.Ordinal))
                { state.OriginalText = label.text; state.WrittenText = Untagged(label.text, kind); label.text = state.WrittenText; }
            }
        }
    }
}
