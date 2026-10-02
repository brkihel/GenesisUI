using System;
using System.Collections.Generic;
using GenesisUI.Foundation;
using GenesisUI.Foundation.Contracts;
using GenesisUI.Host;
using GenesisUI.Theme;
using GenesisUI.Widgets;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GenesisUI.Modules.Windows
{
    /// <summary>
    /// The Esc menu as Diego pictured it (after R-058): the game blurred behind, the options on the
    /// left in GenesisUI's display font, no frames or lines, and a hover that slides the option, lights
    /// it in gold and shows a small knot. Vanilla's pause menu keeps opening, closing and acting,
    /// invisible: the options are the entries vanilla shows on this platform, with its labels and
    /// enabled states (the save cooldown), and every press is its own button or method. The log-out
    /// and quit confirmations take the same place and style.
    /// </summary>
    [GameContract("assembly_valheim", "Menu", "Hide")]
    [GameContract("assembly_valheim", "Menu", "get_instance")]
    [GameContract("assembly_valheim", "Menu", "IsVisible")]
    [GameContract("assembly_valheim", "Menu", "m_menuDialog", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "UnityEngine.Transform")]
    [GameContract("assembly_valheim", "Menu", "m_logoutDialog", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "UnityEngine.Transform")]
    [GameContract("assembly_valheim", "Menu", "m_quitDialog", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "UnityEngine.Transform")]
    [GameContract("assembly_valheim", "Menu", "m_continueButton", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "UnityEngine.UI.Button")]
    [GameContract("assembly_valheim", "Menu", "m_saveButton", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "UnityEngine.UI.Button")]
    [GameContract("assembly_valheim", "Menu", "m_playerListButton", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "UnityEngine.UI.Button")]
    [GameContract("assembly_valheim", "Menu", "m_inviteButton", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "UnityEngine.UI.Button")]
    [GameContract("assembly_valheim", "Menu", "m_settingsButton", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "UnityEngine.UI.Button")]
    [GameContract("assembly_valheim", "Menu", "m_logoutButton", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "UnityEngine.UI.Button")]
    [GameContract("assembly_valheim", "Menu", "m_quitButton", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "UnityEngine.UI.Button")]
    [GameContract("assembly_valheim", "Menu", "m_skipButton", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "UnityEngine.UI.Button")]
    [GameContract("assembly_valheim", "Menu", "lastSaveText", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "TMPro.TMP_Text")]
    [GameContract("assembly_valheim", "Menu", "OnLogoutYes")]
    [GameContract("assembly_valheim", "Menu", "OnLogoutNo")]
    [GameContract("assembly_valheim", "Menu", "OnQuitYes")]
    [GameContract("assembly_valheim", "Menu", "OnQuitNo")]
    [GenesisUI.Foundation.Contracts.ContractDependency(typeof(GenesisUI.Host.ModuleContext), typeof(GenesisUI.Widgets.WindowParts), typeof(GenesisUI.Widgets.WindowCanvas), typeof(GenesisUI.Foundation.VanillaSkin), typeof(GenesisUI.Foundation.GenesisLog), typeof(GenesisUI.Theme.ThemeRuntime), typeof(GenesisUI.Widgets.Ui), typeof(GenesisUI.Theme.ThemeTokens), typeof(GenesisUI.Foundation.Guard))]
    [GameContract("assembly_valheim", "Menu", "m_root", Kind = ContractMemberKind.Field, Static = ContractStatic.Instance, ValueType = "UnityEngine.Transform")]
    internal sealed class PauseMenuModule : IUiModule, IRecoverable
    {
        private const string Owner = "module:win.menu";
        private static readonly string[] NoRegions = new string[0];
        private const float ItemH = 62f, ItemW = 560f;

        private sealed class Entry
        {
            public Option Option;
            public Func<Menu, Button> Vanilla;
            public string Shown;
        }

        private readonly VanillaSkin _skin = new VanillaSkin(Owner);
        private readonly List<Entry> _entries = new List<Entry>();
        private ThemeRuntime _theme;
        private WindowParts _parts;
        private RectTransform _root, _board, _column, _list, _confirm;
        private TextMeshProUGUI _lastSave, _question;
        private Material _blur;
        private Texture2D _shade;
        private bool _applied, _confirmQuit;

        public string Id => "win.menu";
        public string NameToken => "$genesisui_module_pause_menu";
        public IReadOnlyList<string> Regions => new[] { Id };
        public float RefreshRate => 0f;

        /// <summary>IRecoverable: on a fault the pause menu closes.</summary>
        public void CloseVanillaWindow()
        {
            var menu = Menu.instance;
            if (menu != null && menu.m_root != null && menu.m_root.gameObject.activeSelf) menu.Hide();
        }

        public void Build(ModuleContext context)
        {
            _theme = context.Theme;
            _parts = new WindowParts(_theme);
            _applied = false;
        }

        public void Refresh(float deltaSeconds)
        {
            var menu = Menu.instance;
            // IsVisible deliberately lags m_root by up to two frames for input suppression.
            // Drawing must follow the root Show/Hide changed in this frame.
            bool open = menu != null && menu.m_root != null && menu.m_root.gameObject.activeSelf && menu.m_menuDialog != null;
            if (!open)
            {
                if (_applied) Unapply();
                return;
            }
            if (!EnsureBuilt(menu)) return;
            if (!_applied) Apply(menu);
            WindowCanvas.Fit(_board);
            if (_column.localScale != _board.localScale) _column.localScale = _board.localScale;

            bool logout = menu.m_logoutDialog != null && menu.m_logoutDialog.gameObject.activeInHierarchy;
            bool quit = menu.m_quitDialog != null && menu.m_quitDialog.gameObject.activeInHierarchy;
            bool main = menu.m_menuDialog.gameObject.activeInHierarchy && !logout && !quit;
            bool confirm = logout || quit;
            if (_list.gameObject.activeSelf != main) _list.gameObject.SetActive(main);
            if (_confirm.gameObject.activeSelf != confirm) _confirm.gameObject.SetActive(confirm);
            if (confirm)
            {
                _confirmQuit = quit;
                string q = WindowParts.Localize(quit ? "$genesisui_menu_confirm_quit" : "$genesisui_menu_confirm_logout");
                if (_question.text != q) _question.text = q;
            }
            if (!main) return;

            float y = 0f;
            foreach (var e in _entries)
            {
                var v = e.Vanilla(menu);
                bool show = v != null && v.gameObject.activeInHierarchy;
                if (e.Option.Root.gameObject.activeSelf != show) e.Option.Root.gameObject.SetActive(show);
                if (!show) continue;
                if (e.Option.Root.anchoredPosition.y != -y) e.Option.Root.anchoredPosition = new Vector2(0f, -y);
                y += ItemH;
                e.Option.Enabled = v.interactable;
                var label = v.GetComponentInChildren<TMP_Text>();
                string text = label != null ? label.text : e.Shown;
                if (text != e.Shown) { e.Shown = text; e.Option.Label.text = text.ToUpperInvariant(); }
            }
            string save = menu.lastSaveText != null && menu.lastSaveText.gameObject.activeInHierarchy ? menu.lastSaveText.text : "";
            if (_lastSave.text != save) _lastSave.text = save;
            var lr = (RectTransform)_lastSave.transform;
            if (lr.anchoredPosition.y != -(y + 18f)) lr.anchoredPosition = new Vector2(8f, -(y + 18f));
        }

        public void Teardown()
        {
            if (_applied) Unapply();
            if (_root != null) UnityEngine.Object.Destroy(_root.gameObject);
            if (_blur != null) UnityEngine.Object.Destroy(_blur);
            if (_shade != null) UnityEngine.Object.Destroy(_shade);
            _root = null;
            _blur = null;
            _shade = null;
            _entries.Clear();
        }

        private bool EnsureBuilt(Menu menu)
        {
            if (_root != null) return true;
            _root = WindowCanvas.CreateRoot(menu.m_menuDialog, "GenesisUI.PauseMenu", behind: false);
            if (_root == null) return false;
            Draw();
            _root.gameObject.SetActive(false);
            return true;
        }

        private void Apply(Menu menu)
        {
            _applied = true;
            foreach (var t in new[] { menu.m_menuDialog, menu.m_logoutDialog, menu.m_quitDialog })
            {
                if (t == null) continue;
                _skin.Hidden(t.gameObject, interactable: null);
                // The quit and logout dialogs fade their own group in with an animator, which undid the
                // alpha above: they showed, blurred, behind our confirmation (Diego, 2026-10-02). A
                // disabled canvas keeps them undrawn whatever animates them.
                _skin.Undrawn(t.gameObject);
            }
            _root.gameObject.SetActive(true);
            _root.SetAsLastSibling();
            foreach (var e in _entries) e.Option.Reset();
            GenesisLog.Info(Owner, "pause menu shown over vanilla's (hidden), blur " + (_blur != null ? "on" : "off"));
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
            // The game behind, blurred (or only darkened where the blur shader is not available).
            var back = Ui.Fill(Ui.Child(_root, "Backdrop"));
            _blur = _theme.NewBlurMaterial();
            if (_blur != null)
            {
                var raw = back.gameObject.AddComponent<RawImage>();
                raw.material = _blur;
                raw.raycastTarget = true;
                Ui.Image(Ui.Fill(Ui.Child(_root, "Tint")), null, new Color(0f, 0f, 0f, 0.2f));
            }
            else Ui.Image(back, null, new Color(0f, 0f, 0f, 0.62f), raycast: true);
            // A soft shade on the left, where the options are, so they read on any background. It fades
            // out to nothing: a flat shade ended in a hard edge down the screen (R-059).
            var shade = Ui.Child(_root, "Shade");
            shade.anchorMin = Vector2.zero;
            shade.anchorMax = new Vector2(0.6f, 1f);
            shade.offsetMin = shade.offsetMax = Vector2.zero;
            _shade = ShadeTexture();
            var fade = shade.gameObject.AddComponent<RawImage>();
            fade.texture = _shade;
            fade.raycastTarget = false;

            _board = WindowCanvas.Area(_root, "Board");
            _column = Ui.Child(_root, "Column");
            _column.anchorMin = _column.anchorMax = new Vector2(0f, 0.5f);
            _column.pivot = new Vector2(0f, 0.5f);
            _column.anchoredPosition = new Vector2(150f, 0f);
            _column.sizeDelta = new Vector2(ItemW, 640f);

            var title = _parts.Label(_column, "Title", FontRole.Label, 15f, t.TextFlavor, 8f, 0f, ItemW, 22f, TextAlignmentOptions.Left);
            title.text = WindowParts.Localize("$genesisui_menu_title").ToUpperInvariant();
            title.characterSpacing = 12f;
            _list = WindowCanvas.At(_column, "List", 0f, 44f, ItemW, 600f);
            Add("$genesisui_menu_continue", m => m.m_continueButton);
            Add(null, m => m.m_saveButton);
            Add(null, m => m.m_playerListButton);
            Add(null, m => m.m_inviteButton);
            Add(null, m => m.m_settingsButton);
            Add(null, m => m.m_skipButton);
            Add(null, m => m.m_logoutButton);
            Add(null, m => m.m_quitButton);
            _lastSave = _parts.Label(_list, "LastSave", FontRole.Body, 16f, t.TextFlavor, 8f, 0f, ItemW, 24f, TextAlignmentOptions.Left);

            _confirm = WindowCanvas.At(_column, "Confirm", 0f, 44f, ItemW, 300f);
            _question = _parts.Label(_confirm, "Question", FontRole.Display, 30f, t.TextTitle, 8f, 0f, ItemW, 48f, TextAlignmentOptions.Left);
            var yes = new Option(_confirm, _theme, _parts, 0, ConfirmYes);
            yes.Root.anchoredPosition = new Vector2(0f, -70f);
            yes.Label.text = WindowParts.Localize("$genesisui_menu_yes").ToUpperInvariant();
            var no = new Option(_confirm, _theme, _parts, 1, ConfirmNo);
            no.Root.anchoredPosition = new Vector2(0f, -70f - ItemH);
            no.Label.text = WindowParts.Localize("$genesisui_menu_no").ToUpperInvariant();
            _confirm.gameObject.SetActive(false);
        }

        /// <summary>Black, strongest on the left, easing to zero alpha with a flat end (no visible edge).</summary>
        private static Texture2D ShadeTexture()
        {
            const int width = 256;
            var tex = new Texture2D(width, 1, TextureFormat.RGBA32, false)
            {
                name = "GenesisUI.MenuShade",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            var pixels = new Color32[width];
            for (int i = 0; i < width; i++)
            {
                float t = Mathf.Clamp01((i / (float)(width - 1) - 0.25f) / 0.75f);
                float alpha = 0.42f * (1f - Mathf.SmoothStep(0f, 1f, t));
                pixels[i] = new Color32(0, 0, 0, (byte)Mathf.RoundToInt(alpha * 255f));
            }
            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return tex;
        }

        private void Add(string token, Func<Menu, Button> vanilla)
        {
            var e = new Entry { Vanilla = vanilla };
            e.Option = new Option(_list, _theme, _parts, _entries.Count,
                () => { var m = Menu.instance; var b = m != null ? vanilla(m) : null; if (b != null && b.interactable) b.onClick.Invoke(); });
            if (token != null) { e.Shown = WindowParts.Localize(token); e.Option.Label.text = e.Shown.ToUpperInvariant(); }
            e.Option.Root.gameObject.SetActive(false);
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

        /// <summary>One clean option: big display text, and a hover that slides it, lights it and shows a knot.</summary>
        private sealed class Option
        {
            public readonly RectTransform Root;
            public readonly TextMeshProUGUI Label;
            private readonly Hover _hover;
            public bool Enabled { set => _hover.Enabled = value; }

            public Option(RectTransform parent, ThemeRuntime theme, WindowParts parts, int index, Action onClick)
            {
                var t = theme.Tokens;
                Root = WindowCanvas.At(parent, "Option " + index, 0f, index * ItemH, ItemW, ItemH);
                Ui.Image(Root, null, new Color(0f, 0f, 0f, 0f), raycast: true);
                var knot = Ui.Image(WindowCanvas.At(Root, "Knot", 0f, (ItemH - 18f) / 2f, 12f, 18f), theme.Sprite("tab_knot"), Color.white);
                Label = parts.Label(Root, "Label", FontRole.Display, 32f, t.TextTitle, 8f, 0f, ItemW - 8f, ItemH, TextAlignmentOptions.MidlineLeft);
                Label.enableAutoSizing = false;
                _hover = Root.gameObject.AddComponent<Hover>();
                _hover.Init(Label, knot, ThemeRuntime.ToUnity(t.TextTitle), ThemeRuntime.ToUnity(t.AccentGoldBright), onClick);
            }

            public void Reset() => _hover.Reset();
        }

        /// <summary>The hover: eased with unscaled time (the game may be paused behind the menu).</summary>
        private sealed class Hover : GuardedBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
        {
            private TextMeshProUGUI _label;
            private Image _knot;
            private Color _rest, _lit;
            private Action _onClick;
            private bool _over, _enabled = true;
            private float _t;

            internal bool Enabled
            {
                set { if (_enabled == value) return; _enabled = value; Apply(); }
            }

            internal void Init(TextMeshProUGUI label, Image knot, Color rest, Color lit, Action onClick)
            {
                _label = label;
                _knot = knot;
                _rest = rest;
                _lit = lit;
                _onClick = onClick;
                Reset();
            }

            internal void Reset()
            {
                _over = false;
                _t = 0f;
                Apply();
            }

            public void OnPointerEnter(PointerEventData e) => _over = true;
            public void OnPointerExit(PointerEventData e) => _over = false;

            public void OnPointerClick(PointerEventData e)
            {
                if (e.button == PointerEventData.InputButton.Left && _enabled) Guard.Run("module:win.menu", _onClick);
            }

            protected override void OnOwnerUpdate()
            {
                float target = _over && _enabled ? 1f : 0f;
                if (Mathf.Approximately(_t, target)) return;
                _t = Mathf.MoveTowards(_t, target, Time.unscaledDeltaTime * 6f);
                Apply();
            }

            private void Apply()
            {
                if (_label == null) return;
                float e = _t * _t * (3f - 2f * _t); // smoothstep
                var rest = new Color(_rest.r, _rest.g, _rest.b, _enabled ? 0.78f : 0.3f);
                _label.color = Color.Lerp(rest, _lit, e);
                _label.rectTransform.anchoredPosition = new Vector2(8f + 26f * e, _label.rectTransform.anchoredPosition.y);
                _label.characterSpacing = 2f + 4f * e;
                _knot.color = new Color(1f, 1f, 1f, e);
                _knot.rectTransform.anchoredPosition = new Vector2(-6f + 8f * e, _knot.rectTransform.anchoredPosition.y);
            }
        }
    }
}
