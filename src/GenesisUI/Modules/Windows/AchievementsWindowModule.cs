using System.Collections.Generic;
using GenesisUI.Foundation.Contracts;
using GenesisUI.Theme;
using GenesisUI.Widgets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GenesisUI.Modules.Windows
{
    /// <summary>
    /// The Conquistas tab (no concept image: built in the windows' language): the game's achievements
    /// in vanilla's order with the unlocked count, the trophies the player has earned, and the details
    /// of the one picked. Read-only: everything comes from Achievements, the player's trophies and the
    /// item database, exactly what vanilla's panels show.
    /// </summary>
    [GameContract("assembly_valheim", "Achievements", "m_instance", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Static, ValueType = "Achievements")]
    [GameContract("assembly_valheim", "Achievements", "m_achievementLists", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Collections.Generic.List\u00601[[AchievementList, assembly_valheim, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]]")]
    [GameContract("assembly_valheim", "AchievementList", "m_achievements", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Collections.Generic.List\u00601[[Achievement, assembly_valheim, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]]")]
    [GameContract("assembly_valheim", "Achievement", "m_name", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.String")]
    [GameContract("assembly_valheim", "Achievement", "m_description", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.String")]
    [GameContract("assembly_valheim", "Achievement", "m_icon", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "UnityEngine.Sprite")]
    [GameContract("assembly_valheim", "Achievement", "m_iconLocked", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "UnityEngine.Sprite")]
    [GameContract("assembly_valheim", "Achievement", "m_unlocked", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Boolean")]
    [GameContract("assembly_valheim", "Achievement", "m_isSecret", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Boolean")]
    [GameContract("assembly_valheim", "Player", "GetTrophies")]
    [GameContract("assembly_valheim", "ObjectDB", "get_instance")]
    [GameContract("assembly_valheim", "ObjectDB", "GetItemPrefab", Parameters = new[] { "System.String" })]
    [GameContract("assembly_valheim", "ItemDrop", "m_itemData", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "ItemDrop\u002BItemData")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "ItemDrop\u002BItemData", "m_shared", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "ItemDrop\u002BItemData\u002BSharedData")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "ItemDrop\u002BItemData\u002BSharedData", "m_name", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.String")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "ItemDrop\u002BItemData", "GetIcon", Parameters = new string[] {  }, Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Method, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "UnityEngine.Sprite")]
    [GenesisUI.Foundation.Contracts.ContractDependency(typeof(GenesisUI.Theme.ThemeRuntime), typeof(GenesisUI.Widgets.WindowParts), typeof(GenesisUI.Theme.ThemeTokens), typeof(GenesisUI.Widgets.ScrollArea), typeof(GenesisUI.Widgets.WindowCanvas), typeof(GenesisUI.Widgets.Ui), typeof(GenesisUI.Widgets.Frame))]
    internal sealed class AchievementsWindowModule : WindowModuleBase
    {
        private const float AchievementsW = 776f, TrophiesX = 780f, TrophiesW = 444f, DetailsX = 1228f, DetailsW = 352f;
        private const float CardW = 236f, CardH = 60f, Gap = 8f, Trophy = 76f, TrophyPitch = 84f;

        private sealed class Item
        {
            public Sprite Icon;
            public string Name, Text, State;
            public bool Done;
        }

        private readonly List<Item> _achievements = new List<Item>(96);
        private readonly List<Item> _trophies = new List<Item>(96);
        private readonly List<GameObject> _built = new List<GameObject>(200);

        private ScrollArea _achievementScroll, _trophyScroll;
        private TextMeshProUGUI _count, _trophyCount, _name, _state, _text, _empty;
        private Image _icon;
        private GameObject _detailBody;

        protected override WindowShellModule.Tab Tab => WindowShellModule.Tab.Achievements;
        public override string Id => "win.achievements";
        public override string NameToken => "$genesisui_module_achievements_window";

        protected override void Forget()
        {
            _achievements.Clear();
            _trophies.Clear();
            _built.Clear();
        }

        protected override void Draw()
        {
            var t = Theme.Tokens;
            var a = Parts.Panel(Panels, "Achievements", 0f, 0f, AchievementsW, PanelsHeight, "$genesisui_panel_achievements", 54f, 23f, TextAlignmentOptions.Left);
            var tr = Parts.Panel(Panels, "Trophies", TrophiesX, 0f, TrophiesW, PanelsHeight, "$genesisui_panel_trophies", 54f, 23f, TextAlignmentOptions.Left);
            var d = Parts.Panel(Panels, "Details", DetailsX, 0f, DetailsW, PanelsHeight, "$genesisui_panel_details", 0f, 19f, TextAlignmentOptions.Center);
            _count = Parts.Label(a, "Count", FontRole.Body, 17f, t.TextFlavor, 420f, 18f, AchievementsW - 444f, 28f, TextAlignmentOptions.Right);
            _trophyCount = Parts.Label(tr, "Count", FontRole.Body, 17f, t.TextFlavor, 240f, 18f, TrophiesW - 264f, 28f, TextAlignmentOptions.Right);
            _achievementScroll = new ScrollArea(a, "List", 22f, 64f, AchievementsW - 44f, PanelsHeight - 88f, t, CardH + Gap);
            _trophyScroll = new ScrollArea(tr, "List", 22f, 64f, TrophiesW - 44f, PanelsHeight - 88f, t, TrophyPitch);

            const float pad = 24f, w = DetailsW - 2f * pad;
            _empty = Parts.Label(d, "Empty", FontRole.Body, 17f, t.TextFlavor, pad, 290f, w, 60f, TextAlignmentOptions.Center);
            _empty.textWrappingMode = TextWrappingModes.Normal;
            _empty.text = Localize("$genesisui_achievements_pick");
            _detailBody = WindowCanvas.At(d, "Body", 0f, 0f, DetailsW, PanelsHeight).gameObject;
            var body = (RectTransform)_detailBody.transform;
            _icon = Ui.Image(WindowCanvas.At(body, "Icon", pad, 70f, w, 130f), null, Color.white);
            _icon.preserveAspect = true;
            _name = Parts.Label(body, "Name", FontRole.Display, 21f, t.AccentGoldBright, pad, 214f, w, 30f, TextAlignmentOptions.Left);
            _name.characterSpacing = 2f;
            _state = Parts.Label(body, "State", FontRole.Body, 16f, t.TextFlavor, pad, 244f, w, 22f, TextAlignmentOptions.Left);
            Parts.Rule(body, pad, 280f, w);
            _text = Parts.Label(body, "Text", FontRole.Body, 16f, t.TextBody, pad, 294f, w, 340f, TextAlignmentOptions.TopLeft);
            _text.textWrappingMode = TextWrappingModes.Normal;
            _text.enableAutoSizing = false;
            _detailBody.SetActive(false);
        }

        protected override void Opened(InventoryGui gui, Player player)
        {
            Read(player);
            Layout();
            _achievementScroll.ToTop();
            _trophyScroll.ToTop();
        }

        protected override void Tick(InventoryGui gui, Player player, float deltaSeconds) { }

        /// <summary>Vanilla's achievements in vanilla's order (by icon tier 1-9), and the earned trophies.</summary>
        private void Read(Player player)
        {
            _achievements.Clear();
            _trophies.Clear();
            var tiers = new List<Achievement>[10];
            for (int i = 0; i < tiers.Length; i++) tiers[i] = new List<Achievement>();
            var all = Achievements.m_instance;
            if (all != null && all.m_achievementLists != null)
                foreach (var list in all.m_achievementLists)
                    foreach (var a in list.m_achievements)
                    {
                        if (a == null) continue;
                        int tier = 9;
                        if (a.m_iconLocked != null && a.m_iconLocked.name.Length >= 2 && int.TryParse(a.m_iconLocked.name.Substring(0, 2), out var parsed))
                            tier = Mathf.Clamp(parsed, 0, 9);
                        tiers[tier].Add(a);
                    }
            int done = 0;
            for (int tier = 1; tier <= 9; tier++)
                foreach (var a in tiers[tier])
                {
                    bool hidden = a.m_isSecret && !a.m_unlocked;
                    if (a.m_unlocked) done++;
                    _achievements.Add(new Item
                    {
                        Icon = a.m_unlocked ? a.m_icon : a.m_iconLocked,
                        Name = hidden ? "???" : Localize(a.m_name),
                        Text = hidden ? Localize("$genesisui_achievement_secret") : Localize(a.m_description),
                        State = Localize(a.m_unlocked ? "$genesisui_achievement_done" : "$genesisui_achievement_locked"),
                        Done = a.m_unlocked,
                    });
                }
            _count.text = done + " / " + _achievements.Count;

            if (ObjectDB.instance != null)
                foreach (var name in player.GetTrophies())
                {
                    var prefab = ObjectDB.instance.GetItemPrefab(name);
                    var drop = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
                    if (drop == null) continue;
                    var data = drop.m_itemData;
                    string title = Localize(data.m_shared.m_name);
                    _trophies.Add(new Item
                    {
                        Icon = data.GetIcon(),
                        Name = title,
                        Text = Localize(data.m_shared.m_name + "_lore"),
                        State = Localize("$genesisui_trophy"),
                        Done = true,
                    });
                }
            _trophyCount.text = _trophies.Count.ToString();
        }

        private void Layout()
        {
            var t = Theme.Tokens;
            foreach (var go in _built) Object.Destroy(go);
            _built.Clear();
            for (int i = 0; i < _achievements.Count; i++)
            {
                var item = _achievements[i];
                int col = i % 3, row = i / 3;
                var card = WindowCanvas.At(_achievementScroll.Content, "Achievement " + i, col * (CardW + Gap), row * (CardH + Gap), CardW, CardH);
                Frame.Dress(card, Theme, "keycap_wide", "Windows", CardH);
                var icon = Ui.Image(WindowCanvas.At(card, "Icon", 8f, 8f, 44f, 44f), item.Icon, item.Done ? Color.white : new Color(1f, 1f, 1f, 0.45f));
                icon.preserveAspect = true;
                var name = Parts.Label(card, "Name", FontRole.Body, 16f, item.Done ? t.TextTitle : t.TextFlavor, 60f, 6f, CardW - 68f, 24f, TextAlignmentOptions.Left);
                name.text = item.Name;
                Parts.Label(card, "State", FontRole.Body, 13f, item.Done ? t.StatePositive : t.TextFlavor, 60f, 30f, CardW - 68f, 20f, TextAlignmentOptions.Left).text = item.State;
                Parts.Clickable(card, "achievement", () => Show(item));
                _built.Add(card.gameObject);
            }
            _achievementScroll.ContentHeight = ((_achievements.Count + 2) / 3) * (CardH + Gap);

            int perRow = Mathf.Max(1, Mathf.FloorToInt((TrophiesW - 54f) / TrophyPitch));
            for (int i = 0; i < _trophies.Count; i++)
            {
                var item = _trophies[i];
                int col = i % perRow, row = i / perRow;
                var cell = WindowCanvas.At(_trophyScroll.Content, "Trophy " + i, col * TrophyPitch, row * TrophyPitch, Trophy, Trophy);
                Frame.Dress(cell, Theme, "hotslot", "Windows", Trophy);
                var icon = Ui.Image(WindowCanvas.At(cell, "Icon", 10f, 10f, Trophy - 20f, Trophy - 20f), item.Icon, Color.white);
                icon.preserveAspect = true;
                Parts.Clickable(cell, "trophy", () => Show(item));
                _built.Add(cell.gameObject);
            }
            _trophyScroll.ContentHeight = ((_trophies.Count + perRow - 1) / perRow) * TrophyPitch;
            if (_trophies.Count == 0)
            {
                var none = Parts.Label(_trophyScroll.Content, "None", FontRole.Body, 17f, t.TextFlavor, 10f, 30f, TrophiesW - 74f, 80f, TextAlignmentOptions.Center);
                none.textWrappingMode = TextWrappingModes.Normal;
                none.text = Localize("$genesisui_trophies_none");
                _built.Add(none.gameObject);
            }
            _detailBody.SetActive(false);
            _empty.gameObject.SetActive(true);
        }

        private void Show(Item item)
        {
            _detailBody.SetActive(true);
            _empty.gameObject.SetActive(false);
            _icon.sprite = item.Icon;
            _name.text = item.Name.ToUpperInvariant();
            _state.text = item.State;
            _state.color = ThemeRuntime.ToUnity(item.Done ? Theme.Tokens.StatePositive : Theme.Tokens.TextFlavor);
            _text.text = item.Text;
        }
    }
}
