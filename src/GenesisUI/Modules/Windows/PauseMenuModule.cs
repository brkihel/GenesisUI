using System.Collections.Generic;
using GenesisUI.Foundation;
using GenesisUI.Foundation.Contracts;
using GenesisUI.Host;
using GenesisUI.Theme;
using GenesisUI.Widgets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GenesisUI.Modules.Windows
{
    /// <summary>
    /// The Esc menu in the windows' language (D-032): vanilla's pause menu keeps opening, closing and
    /// acting, invisible; GenesisUI's card shows the entries vanilla shows on this platform (continue,
    /// save with its cooldown, players, invite, settings, log out, quit) with the last save time, and
    /// the log-out and quit confirmations. Every press is vanilla's own button or method. The game's
    /// settings screen keeps vanilla's look for now.
    /// </summary>
    [GameContract("assembly_valheim", "Menu", "Hide")]
    [GameContract("assembly_valheim", "Menu", "get_instance")]
    [GameContract("assembly_valheim", "Menu", "IsVisible")]
    [GameContract("assembly_valheim", "Menu", "m_menuDialog")]
    [GameContract("assembly_valheim", "Menu", "m_logoutDialog")]
    [GameContract("assembly_valheim", "Menu", "m_quitDialog")]
    [GameContract("assembly_valheim", "Menu", "m_continueButton")]
    [GameContract("assembly_valheim", "Menu", "m_saveButton")]
    [GameContract("assembly_valheim", "Menu", "m_playerListButton")]
    [GameContract("assembly_valheim", "Menu", "m_inviteButton")]
    [GameContract("assembly_valheim", "Menu", "m_settingsButton")]
    [GameContract("assembly_valheim", "Menu", "m_logoutButton")]
    [GameContract("assembly_valheim", "Menu", "m_quitButton")]
    [GameContract("assembly_valheim", "Menu", "m_skipButton")]
    [GameContract("assembly_valheim", "Menu", "lastSaveText")]
    [GameContract("assembly_valheim", "Menu", "OnLogoutYes")]
    [GameContract("assembly_valheim", "Menu", "OnLogoutNo")]
    [GameContract("assembly_valheim", "Menu", "OnQuitYes")]
    [GameContract("assembly_valheim", "Menu", "OnQuitNo")]
    internal sealed class PauseMenuModule : IUiModule, IRecoverable
    {
        /// <summary>IRecoverable: on a fault the pause menu closes.</summary>
        public void CloseVanillaWindow()
        {
            var menu = Menu.instance;
            if (menu != null && Menu.IsVisible()) menu.Hide();
        }

        private const string Owner = "module:win.menu";
        private static readonly string[] NoRegions = new string[0];
        private const float W = 420f, RowH = 50f, Gap = 10f;

        private sealed class Entry
        {
            public Button Ours;
            public TextMeshProUGUI Label;
            public System.Func<Menu, Button> Vanilla;
        }

        private readonly VanillaSkin _skin = new VanillaSkin(Owner);
        private readonly List<Entry> _entries = new List<Entry>();
        private ThemeRuntime _theme;
        private WindowParts _parts;
        private RectTransform _root, _board, _menuCard, _confirmCard;
        private TextMeshProUGUI _lastSave, _confirmText;
        private bool _applied, _confirmQuit;

        public string Id => "win.menu";
        public string NameToken => "$genesisui_module_pause_menu";
        public IReadOnlyList<string> Regions => NoRegions;
        public float RefreshRate => 0f;

        public void Build(ModuleContext context)
        {
            _theme = context.Theme;
            _parts = new WindowParts(_theme);
            _applied = false;
        }

        public void Refresh(float deltaSeconds)
        {
            var menu = Menu.instance;
            bool open = menu != null && Menu.IsVisible() && menu.m_menuDialog != null;
            if (!open)
            {
                if (_applied) Unapply();
                return;
            }
            if (!EnsureBuilt(menu)) return;
            if (!_applied) Apply(menu);
            WindowCanvas.Fit(_board);

            bool logout = menu.m_logoutDialog != null && menu.m_logoutDialog.gameObject.activeInHierarchy;
            bool quit = menu.m_quitDialog != null && menu.m_quitDialog.gameObject.activeInHierarchy;
            bool main = menu.m_menuDialog.gameObject.activeInHierarchy && !logout && !quit;
            if (_menuCard.gameObject.activeSelf != main) _menuCard.gameObject.SetActive(main);
            bool confirm = logout || quit;
            if (_confirmCard.gameObject.activeSelf != confirm) _confirmCard.gameObject.SetActive(confirm);
            if (confirm)
            {
                _confirmQuit = quit;
                string text = WindowParts.Localize(quit ? "$genesisui_menu_confirm_quit" : "$genesisui_menu_confirm_logout");
                if (_confirmText.text != text) _confirmText.text = text;
            }
            if (!main) return;

            // Mirror vanilla's entries: shown when vanilla shows them, dimmed when vanilla disables them
            // (the save cooldown), with vanilla's own label.
            float y = 70f;
            foreach (var e in _entries)
            {
                var v = e.Vanilla(menu);
                bool show = v != null && v.gameObject.activeInHierarchy;
                if (e.Ours.gameObject.activeSelf != show) e.Ours.gameObject.SetActive(show);
                if (!show) continue;
                ((RectTransform)e.Ours.transform).anchoredPosition = new Vector2(40f, -y);
                y += RowH + Gap;
                if (e.Ours.interactable != v.interactable) e.Ours.interactable = v.interactable;
                var label = v.GetComponentInChildren<TMP_Text>();
                if (label != null && e.Label.text != label.text) e.Label.text = label.text;
            }
            string save = menu.lastSaveText != null && menu.lastSaveText.gameObject.activeInHierarchy ? menu.lastSaveText.text : "";
            if (_lastSave.text != save) _lastSave.text = save;
            var lr = (RectTransform)_lastSave.transform;
            lr.anchoredPosition = new Vector2(24f, -(y + 4f));
            float height = y + 50f;
            if (!Mathf.Approximately(_menuCard.sizeDelta.y, height))
            {
                _menuCard.sizeDelta = new Vector2(W, height);
                _menuCard.anchoredPosition = new Vector2((WindowCanvas.Design.x - W) / 2f, -(WindowCanvas.Design.y - height) / 2f);
            }
        }

        public void Teardown()
        {
            if (_applied) Unapply();
            if (_root != null) Object.Destroy(_root.gameObject);
            _root = null;
            _entries.Clear();
        }

        private bool EnsureBuilt(Menu menu)
        {
            if (_root != null) return true;
            _root = WindowCanvas.CreateRoot(menu.m_menuDialog, "GenesisUI.PauseMenu", behind: false);
            if (_root == null) return false;
            _board = WindowCanvas.Area(_root, "Board");
            Draw();
            _root.gameObject.SetActive(false);
            return true;
        }

        private void Apply(Menu menu)
        {
            _applied = true;
            foreach (var go in new[] { menu.m_menuDialog, menu.m_logoutDialog, menu.m_quitDialog })
            {
                if (go == null) continue;
                var g = _skin.Group(go.gameObject);
                g.alpha = 0f;
                g.blocksRaycasts = false;
            }
            _root.gameObject.SetActive(true);
            _root.SetAsLastSibling();
            GenesisLog.Info(Owner, "pause menu shown over vanilla's (hidden)");
        }

        private void Unapply()
        {
            _applied = false;
            _skin.Restore();
            if (_root != null) _root.gameObject.SetActive(false);
        }

        private void Draw()
        {
            var t = _theme.Tokens;
            _menuCard = WindowCanvas.At(_board, "Menu", (WindowCanvas.Design.x - W) / 2f, 200f, W, 460f);
            Ui.Image(_menuCard, null, new Color(0f, 0f, 0f, 0f), raycast: true);
            Frame.Dress(_menuCard, _theme, "window_panel", "Windows");
            var title = _parts.Label(_menuCard, "Title", FontRole.Display, 22f, t.AccentGoldBright, 40f, 14f, W - 80f, 34f, TextAlignmentOptions.Center);
            title.text = WindowParts.Localize("$genesisui_menu_title").ToUpperInvariant();
            title.characterSpacing = 6f;
            _parts.Rule(_menuCard, W * 0.2f, 54f, W * 0.6f);
            Add("$genesisui_menu_continue", m => m.m_continueButton);
            Add(null, m => m.m_saveButton);
            Add(null, m => m.m_playerListButton);
            Add(null, m => m.m_inviteButton);
            Add(null, m => m.m_settingsButton);
            Add(null, m => m.m_skipButton);
            Add(null, m => m.m_logoutButton);
            Add(null, m => m.m_quitButton);
            _lastSave = _parts.Label(_menuCard, "LastSave", FontRole.Body, 15f, t.TextFlavor, 24f, 400f, W - 48f, 22f, TextAlignmentOptions.Center);

            const float cw = 520f, ch = 200f;
            _confirmCard = WindowCanvas.At(_board, "Confirm", (WindowCanvas.Design.x - cw) / 2f, (WindowCanvas.Design.y - ch) / 2f, cw, ch);
            Ui.Image(_confirmCard, null, new Color(0f, 0f, 0f, 0f), raycast: true);
            Frame.Dress(_confirmCard, _theme, "window_panel", "Windows");
            _confirmText = _parts.Label(_confirmCard, "Text", FontRole.Display, 20f, t.TextTitle, 40f, 30f, cw - 80f, 70f, TextAlignmentOptions.Center);
            _confirmText.textWrappingMode = TextWrappingModes.Normal;
            _parts.Button(_confirmCard, "No", 40f, ch - 74f, 200f, 44f, "$genesisui_menu_no", 18f, "menu confirm no", ConfirmNo, out _);
            _parts.Button(_confirmCard, "Yes", cw - 40f - 200f, ch - 74f, 200f, 44f, "$genesisui_menu_yes", 18f, "menu confirm yes", ConfirmYes, out var yes);
            yes.font = _theme.Font(FontRole.Display);
            _confirmCard.gameObject.SetActive(false);
        }

        private void Add(string token, System.Func<Menu, Button> vanilla)
        {
            var e = new Entry { Vanilla = vanilla };
            e.Ours = _parts.Button(_menuCard, "Entry " + _entries.Count, 40f, 70f + _entries.Count * (RowH + Gap), W - 80f, RowH, token, 19f, "menu entry",
                () => { var m = Menu.instance; var b = m != null ? vanilla(m) : null; if (b != null && b.interactable) b.onClick.Invoke(); }, out e.Label);
            e.Ours.gameObject.SetActive(false);
            _entries.Add(e);
        }

        private void ConfirmYes()
        {
            var m = Menu.instance;
            if (m == null) return;
            if (_confirmQuit) m.OnQuitYes(); else m.OnLogoutYes();
        }

        private void ConfirmNo()
        {
            var m = Menu.instance;
            if (m == null) return;
            if (_confirmQuit) m.OnQuitNo(); else m.OnLogoutNo();
        }
    }
}
