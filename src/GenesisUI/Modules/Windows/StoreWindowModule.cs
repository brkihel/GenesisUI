using System.Collections.Generic;
using GenesisUI.Foundation;
using GenesisUI.Foundation.Contracts;
using GenesisUI.Host;
using GenesisUI.Theme;
using GenesisUI.Widgets;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GenesisUI.Modules.Windows
{
    /// <summary>
    /// The traders (Haldor, Hildir, the Bog Witch and modded ones) in the windows' language (D-032):
    /// the goods with their prices, the details of the one picked, the player's coins, Comprar and
    /// Vender. Vanilla's store stays open and invisible and does every trade: our rows press its rows,
    /// our buttons press its buy and sell buttons, and the sell line names what vanilla will sell.
    /// </summary>
    [GameContract("assembly_valheim", "StoreGui", "IsVisible")]
    [GameContract("assembly_valheim", "StoreGui", "get_instance")]
    [GameContract("assembly_valheim", "StoreGui", "m_rootPanel")]
    [GameContract("assembly_valheim", "StoreGui", "m_buyButton")]
    [GameContract("assembly_valheim", "StoreGui", "m_sellButton")]
    [GameContract("assembly_valheim", "StoreGui", "m_coinText")]
    [GameContract("assembly_valheim", "StoreGui", "m_itemList")]
    [GameContract("assembly_valheim", "StoreGui", "m_trader")]
    [GameContract("assembly_valheim", "StoreGui", "GetSellableItem")]
    [GameContract("assembly_valheim", "StoreGui", "Hide")]
    [GameContract("assembly_valheim", "Trader", "GetAvailableItems")]
    [GameContract("assembly_valheim", "Trader", "m_name")]
    [GameContract("assembly_valheim", "Trader+TradeItem", "m_prefab")]
    [GameContract("assembly_valheim", "Trader+TradeItem", "m_icon")]
    [GameContract("assembly_valheim", "Trader+TradeItem", "m_name")]
    [GameContract("assembly_valheim", "Trader+TradeItem", "m_price")]
    [GameContract("assembly_valheim", "Trader+TradeItem", "m_stack")]
    internal sealed class StoreWindowModule : IUiModule, IRecoverable
    {
        /// <summary>IRecoverable: on a fault the store closes.</summary>
        public void CloseVanillaWindow()
        {
            var store = StoreGui.instance;
            if (store != null && StoreGui.IsVisible()) store.Hide();
        }

        private const string Owner = "module:win.store";
        private static readonly string[] NoRegions = new string[0];
        private const float Top = 102f, Height = 673f;
        private const float ListX = 250f, ListW = 720f, DetailsX = 974f, DetailsW = 356f;
        private const float RowH = 58f, RowGap = 6f;

        private sealed class Row
        {
            public RectTransform Root;
            public Image Icon, Selection;
            public TextMeshProUGUI Name, Price;
            public GameObject Source, Mark;
            public Trader.TradeItem Item;
        }

        private readonly VanillaSkin _skin = new VanillaSkin(Owner);
        private readonly List<Row> _rows = new List<Row>(32);
        private readonly List<StatRow> _stats = new List<StatRow>(12);

        private ThemeRuntime _theme;
        private WindowParts _parts;
        private RectTransform _root, _board;
        private ScrollArea _list;
        private TextMeshProUGUI _trader, _coins, _name, _description, _price, _sell, _buyLabel;
        private Image _icon;
        private GameObject _detailBody;
        private Button _buy, _sellButton;
        private bool _applied;
        private GameObject _firstElement;
        private int _elementCount = -1;
        private Row _selected;
        private float _sellIn;

        private AccessTools.FieldRef<StoreGui, List<GameObject>> _itemList;
        private AccessTools.FieldRef<StoreGui, Trader> _traderRef;
        private System.Func<StoreGui, ItemDrop.ItemData> _sellable;

        public string Id => "win.store";
        public string NameToken => "$genesisui_module_store_window";
        public IReadOnlyList<string> Regions => NoRegions;
        public float RefreshRate => 0f;

        public void Build(ModuleContext context)
        {
            _theme = context.Theme;
            _parts = new WindowParts(_theme);
            _itemList = AccessTools.FieldRefAccess<StoreGui, List<GameObject>>("m_itemList");
            _traderRef = AccessTools.FieldRefAccess<StoreGui, Trader>("m_trader");
            _sellable = AccessTools.MethodDelegate<System.Func<StoreGui, ItemDrop.ItemData>>(AccessTools.Method(typeof(StoreGui), "GetSellableItem"));
            _applied = false;
            _elementCount = -1;
        }

        public void Refresh(float deltaSeconds)
        {
            var store = StoreGui.instance;
            bool open = store != null && store.m_rootPanel != null && store.m_rootPanel.activeSelf && Player.m_localPlayer != null && _traderRef(store) != null;
            if (!open)
            {
                if (_applied) Unapply();
                return;
            }
            if (!EnsureBuilt(store)) return;
            if (!_applied) Apply(store);
            WindowCanvas.Fit(_board);

            var elements = _itemList(store);
            var first = elements != null && elements.Count > 0 ? elements[0] : null;
            if (elements == null || elements.Count != _elementCount || first != _firstElement) Rebuild(store, elements);
            UpdateSelection();
            UpdateButtons(store, deltaSeconds);
        }

        public void Teardown()
        {
            if (_applied) Unapply();
            if (_root != null) Object.Destroy(_root.gameObject);
            _root = null;
            _rows.Clear();
            _elementCount = -1;
            _firstElement = null;
            _selected = null;
        }

        private bool EnsureBuilt(StoreGui store)
        {
            if (_root != null) return true;
            _root = WindowCanvas.CreateRoot(store, "GenesisUI.Store", behind: false);
            if (_root == null) return false;
            Ui.Image(Ui.Fill(Ui.Child(_root, "Dim")), null, new Color(0f, 0f, 0f, 0.35f));
            _board = WindowCanvas.Area(_root, "Board");
            Draw();
            _root.gameObject.SetActive(false);
            return true;
        }

        private void Apply(StoreGui store)
        {
            _applied = true;
            var group = _skin.Group(store.m_rootPanel);
            group.alpha = 0f;
            group.blocksRaycasts = false;
            _root.gameObject.SetActive(true);
            _elementCount = -1;
            var trader = _traderRef(store);
            _trader.text = trader != null ? Localize(trader.m_name).ToUpperInvariant() : "";
            GenesisLog.Info(Owner, "store shown over vanilla's (hidden)");
        }

        private void Unapply()
        {
            _applied = false;
            _skin.Restore();
            if (_root != null) _root.gameObject.SetActive(false);
            _selected = null;
        }

        /// <summary>Vanilla rebuilt its list (opening, after a sale): our rows follow its elements.</summary>
        private void Rebuild(StoreGui store, List<GameObject> elements)
        {
            _elementCount = elements != null ? elements.Count : 0;
            _firstElement = _elementCount > 0 ? elements[0] : null;
            var trader = _traderRef(store);
            var items = trader != null ? trader.GetAvailableItems() : null;
            while (_rows.Count < _elementCount) _rows.Add(MakeRow(_rows.Count));
            for (int i = 0; i < _rows.Count; i++)
            {
                var row = _rows[i];
                bool show = i < _elementCount && items != null && i < items.Count;
                row.Root.gameObject.SetActive(show);
                if (!show) { row.Source = null; row.Item = null; continue; }
                row.Source = elements[i];
                var mark = elements[i].transform.Find("selected");
                row.Mark = mark != null ? mark.gameObject : null;
                row.Item = items[i];
                var item = items[i];
                row.Icon.sprite = item.m_icon != null ? item.m_icon : item.m_prefab != null ? item.m_prefab.m_itemData.GetIcon() : null;
                string name = Localize(item.m_prefab != null ? item.m_prefab.m_itemData.m_shared.m_name : item.m_name);
                if (item.m_stack > 1) name += " x" + item.m_stack;
                row.Name.text = name;
                row.Price.text = item.m_price.ToString();
            }
            _list.ContentHeight = _elementCount * (RowH + RowGap);
            _selected = null;
        }

        private void UpdateSelection()
        {
            Row selected = null;
            foreach (var row in _rows)
            {
                if (row.Source == null) continue;
                bool on = row.Mark != null && row.Mark.activeSelf;
                if (row.Selection.enabled != on) row.Selection.enabled = on;
                if (on) selected = row;
            }
            if (selected == _selected) return;
            _selected = selected;
            ShowDetails(selected != null ? selected.Item : null);
        }

        private void ShowDetails(Trader.TradeItem item)
        {
            _detailBody.SetActive(item != null);
            if (item == null) return;
            var data = item.m_prefab != null ? item.m_prefab.m_itemData : null;
            _icon.sprite = item.m_icon != null ? item.m_icon : data != null ? data.GetIcon() : null;
            string name = Localize(data != null ? data.m_shared.m_name : item.m_name);
            if (item.m_stack > 1) name += " x" + item.m_stack;
            _name.text = name.ToUpperInvariant();
            _description.text = data != null ? Localize(data.m_shared.m_description) : "";
            _price.text = Localize("$genesisui_store_price") + " " + item.m_price;
        }

        private void UpdateButtons(StoreGui store, float dt)
        {
            if (_buy.interactable != store.m_buyButton.interactable) _buy.interactable = store.m_buyButton.interactable;
            if (_sellButton.interactable != store.m_sellButton.interactable) _sellButton.interactable = store.m_sellButton.interactable;
            string coins = store.m_coinText != null ? store.m_coinText.text : "";
            if (_coins.text != coins) _coins.text = coins;
            _sellIn -= dt;
            if (_sellIn > 0f) return;
            _sellIn = 0.5f;
            // Vanilla sells its first valuable item (not coins): say which, and for how much.
            var sellable = _sellable(store);
            _sell.text = sellable != null
                ? Localize("$genesisui_store_sell_next").Replace("{0}", (sellable.m_stack > 1 ? sellable.m_stack + "x " : "") + Localize(sellable.m_shared.m_name))
                    .Replace("{1}", (sellable.m_shared.m_value * sellable.m_stack).ToString())
                : Localize("$genesisui_store_nothing_to_sell");
        }

        // ------------------------------------------------------------------ building

        private void Draw()
        {
            var t = _theme.Tokens;
            var panels = WindowCanvas.At(_board, "Panels", 0f, Top, WindowCanvas.Design.x, Height);
            var list = _parts.Panel(panels, "Store", ListX, 0f, ListW, Height, "$genesisui_store_title", 54f, 24f, TextAlignmentOptions.Left);
            var details = _parts.Panel(panels, "Details", DetailsX, 0f, DetailsW, Height, "$genesisui_panel_details", 0f, 19f, TextAlignmentOptions.Center);
            _trader = _parts.Label(list, "Trader", FontRole.Body, 17f, t.TextFlavor, 330f, 18f, ListW - 480f, 28f, TextAlignmentOptions.Right);
            _parts.Button(list, "Close", ListW - 24f - 110f, 16f, 110f, 32f, "$genesisui_hint_close", 16f, "store close",
                () => { var s = StoreGui.instance; if (s != null) s.Hide(); }, out _);
            _list = new ScrollArea(list, "Goods", 22f, 64f, ListW - 44f, Height - 150f, t, RowH + RowGap);

            // Coins and selling, at the bottom of the goods panel.
            var coinLabel = _parts.Label(list, "CoinsLabel", FontRole.Label, 14f, t.AccentGoldBright, 26f, Height - 76f, 200f, 20f, TextAlignmentOptions.Left);
            coinLabel.text = Localize("$genesisui_store_coins").ToUpperInvariant();
            coinLabel.characterSpacing = 3f;
            _coins = _parts.Label(list, "Coins", FontRole.Display, 22f, t.TextTitle, 26f, Height - 56f, 200f, 30f, TextAlignmentOptions.Left);
            _sell = _parts.Label(list, "Sell", FontRole.Body, 15f, t.TextFlavor, 240f, Height - 78f, ListW - 440f, 50f, TextAlignmentOptions.MidlineRight);
            _sell.textWrappingMode = TextWrappingModes.Normal;
            _sellButton = _parts.Button(list, "SellButton", ListW - 24f - 180f, Height - 74f, 180f, 44f, "$genesisui_store_sell", 18f, "store sell",
                () => { var s = StoreGui.instance; if (s != null && s.m_sellButton.interactable) s.m_sellButton.onClick.Invoke(); }, out _);

            const float pad = 24f, w = DetailsW - 2f * pad;
            _detailBody = WindowCanvas.At(details, "Body", 0f, 0f, DetailsW, Height).gameObject;
            var body = (RectTransform)_detailBody.transform;
            _icon = Ui.Image(WindowCanvas.At(body, "Icon", pad, 70f, w, 130f), null, Color.white);
            _icon.preserveAspect = true;
            _name = _parts.Label(body, "Name", FontRole.Display, 21f, t.AccentGoldBright, pad, 212f, w, 30f, TextAlignmentOptions.Left);
            _price = _parts.Label(body, "Price", FontRole.Body, 17f, t.TextTitle, pad, 244f, w, 24f, TextAlignmentOptions.Left);
            _parts.Rule(body, pad, 282f, w);
            _description = _parts.Label(body, "Description", FontRole.Body, 16f, t.TextBody, pad, 296f, w, 260f, TextAlignmentOptions.TopLeft);
            _description.textWrappingMode = TextWrappingModes.Normal;
            _description.enableAutoSizing = false;
            _buy = _parts.Button(body, "Buy", pad, Height - 74f, w, 44f, "$genesisui_store_buy", 20f, "store buy",
                () => { var s = StoreGui.instance; if (s != null && s.m_buyButton.interactable) s.m_buyButton.onClick.Invoke(); }, out _buyLabel);
            _buyLabel.font = _theme.Font(FontRole.Display);
            _detailBody.SetActive(false);
        }

        private Row MakeRow(int i)
        {
            var t = _theme.Tokens;
            float w = _list.Content.sizeDelta.x;
            var row = new Row { Root = WindowCanvas.At(_list.Content, "Good " + i, 0f, i * (RowH + RowGap), w, RowH) };
            Frame.Dress(row.Root, _theme, "keycap_wide", "Windows", RowH);
            row.Selection = Ui.Image(Ui.Fill(Ui.Child(row.Root, "Selected"), 3f, 3f, 3f, 3f), null, ThemeRuntime.ToUnity(t.AccentGold).WithA(0.14f));
            row.Selection.enabled = false;
            row.Icon = Ui.Image(WindowCanvas.At(row.Root, "Icon", 9f, 7f, 44f, 44f), null, Color.white);
            row.Icon.preserveAspect = true;
            row.Name = _parts.Label(row.Root, "Name", FontRole.Body, 18f, t.TextTitle, 64f, 0f, w - 200f, RowH, TextAlignmentOptions.MidlineLeft);
            row.Price = _parts.Label(row.Root, "Price", FontRole.Display, 18f, t.AccentGoldBright, w - 130f, 0f, 116f, RowH, TextAlignmentOptions.MidlineRight);
            _parts.Clickable(row.Root, "store good", () => { if (row.Source != null) { var b = row.Source.GetComponent<Button>(); if (b != null) b.onClick.Invoke(); } });
            return row;
        }

        private static string Localize(string text) => WindowParts.Localize(text);
    }
}
