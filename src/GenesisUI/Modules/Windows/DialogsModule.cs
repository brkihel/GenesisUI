using System.Collections.Generic;
using GenesisUI.Foundation;
using GenesisUI.Foundation.Contracts;
using GenesisUI.Host;
using GenesisUI.Theme;
using GenesisUI.Widgets;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GenesisUI.Modules.Windows
{
    /// <summary>
    /// Vanilla's small dialogs in the windows' language (D-032): splitting a stack, choosing a style,
    /// reading rune stones and the ravens, and naming a sign or a portal. Each vanilla dialog keeps
    /// opening, closing and acting itself, invisible; ours mirrors its texts and hands every press to
    /// its own buttons and methods (the split slider moves vanilla's slider, typing goes to vanilla's
    /// input field). The game's opening intro keeps vanilla's look.
    /// </summary>
    [GameContract("assembly_valheim", "TextInput", "Hide")]
    [GameContract("assembly_valheim", "InventoryGui", "Hide")]
    [GameContract("assembly_valheim", "InventoryGui", "m_splitDialog")]
    [GameContract("assembly_valheim", "InventoryGui", "m_variantDialog")]
    [GameContract("assembly_valheim", "SplitDialog", "get_IsActive")]
    [GameContract("assembly_valheim", "SplitDialog", "m_splitSlider")]
    [GameContract("assembly_valheim", "SplitDialog", "m_panel")]
    [GameContract("assembly_valheim", "SplitDialog", "m_splitIcon")]
    [GameContract("assembly_valheim", "SplitDialog", "m_splitIconName")]
    [GameContract("assembly_valheim", "SplitDialog", "m_splitAmount")]
    [GameContract("assembly_valheim", "SplitDialog", "m_splitOkButton")]
    [GameContract("assembly_valheim", "SplitDialog", "m_splitCancelButton")]
    [GameContract("assembly_valheim", "VariantDialog", "m_elements")]
    [GameContract("assembly_valheim", "VariantDialog", "OnClose")]
    [GameContract("assembly_valheim", "TextViewer", "get_instance")]
    [GameContract("assembly_valheim", "TextViewer", "IsVisible")]
    [GameContract("assembly_valheim", "TextViewer", "m_root")]
    [GameContract("assembly_valheim", "TextViewer", "m_ravenRoot")]
    [GameContract("assembly_valheim", "TextViewer", "m_topic")]
    [GameContract("assembly_valheim", "TextViewer", "m_text")]
    [GameContract("assembly_valheim", "TextViewer", "m_ravenTopic")]
    [GameContract("assembly_valheim", "TextViewer", "m_ravenText")]
    [GameContract("assembly_valheim", "TextViewer", "Hide")]
    [GameContract("assembly_valheim", "TextInput", "get_instance")]
    [GameContract("assembly_valheim", "TextInput", "IsVisible")]
    [GameContract("assembly_valheim", "TextInput", "m_panel")]
    [GameContract("assembly_valheim", "TextInput", "m_topic")]
    [GameContract("assembly_valheim", "TextInput", "m_inputField")]
    [GameContract("assembly_valheim", "TextInput", "OnEnter")]
    [GameContract("assembly_valheim", "TextInput", "OnCancel")]
    internal sealed class DialogsModule : IUiModule, IRecoverable
    {
        /// <summary>IRecoverable: on a fault the open dialogs close (split and style close with the inventory).</summary>
        public void CloseVanillaWindow()
        {
            var gui = InventoryGui.instance;
            if (gui != null && (_split.Shown || _variant.Shown)) gui.Hide();
            var tv = TextViewer.instance;
            if (tv != null && tv.IsVisible()) tv.Hide();
            var input = TextInput.instance;
            if (input != null && TextInput.IsVisible()) input.Hide();
        }

        private const string Owner = "module:win.dialogs";
        private static readonly string[] NoRegions = new string[0];

        /// <summary>One mirrored dialog: its own root on the vanilla dialog's canvas and its own veil.</summary>
        private sealed class Dialog
        {
            public string Name;
            public RectTransform Root, Board;
            public VanillaSkin Skin;
            public bool Shown;
        }

        private ThemeRuntime _theme;
        private WindowParts _parts;
        private readonly Dialog _split = new Dialog { Name = "Split" };
        private readonly Dialog _variant = new Dialog { Name = "Variant" };
        private readonly Dialog _reader = new Dialog { Name = "Reader" };
        private readonly Dialog _raven = new Dialog { Name = "Raven" };
        private readonly Dialog _input = new Dialog { Name = "Input" };

        // Split.
        private Image _splitIcon;
        private TextMeshProUGUI _splitName, _splitAmount;
        private Slider _splitSlider;
        private bool _syncingSlider;
        // Variant.
        private readonly List<(RectTransform Root, Image Icon)> _variantCells = new List<(RectTransform, Image)>();
        private GameObject _variantFirst;
        private int _variantCount = -1;
        // Readers.
        private TextMeshProUGUI _readerTopic, _readerText, _ravenTopic, _ravenText;
        private string _shownReader, _shownRaven;
        // Input.
        private TextMeshProUGUI _inputTopic, _inputText;

        private AccessTools.FieldRef<SplitDialog, RectTransform> _splitPanel;
        private AccessTools.FieldRef<SplitDialog, Image> _splitIconRef;
        private AccessTools.FieldRef<SplitDialog, TMP_Text> _splitNameRef, _splitAmountRef;
        private AccessTools.FieldRef<SplitDialog, Button> _splitOk, _splitCancel;
        private AccessTools.FieldRef<VariantDialog, List<GameObject>> _variantElements;

        public string Id => "win.dialogs";
        public string NameToken => "$genesisui_module_dialogs";
        public IReadOnlyList<string> Regions => NoRegions;
        public float RefreshRate => 0f;

        public void Build(ModuleContext context)
        {
            _theme = context.Theme;
            _parts = new WindowParts(_theme);
            _splitPanel = AccessTools.FieldRefAccess<SplitDialog, RectTransform>("m_panel");
            _splitIconRef = AccessTools.FieldRefAccess<SplitDialog, Image>("m_splitIcon");
            _splitNameRef = AccessTools.FieldRefAccess<SplitDialog, TMP_Text>("m_splitIconName");
            _splitAmountRef = AccessTools.FieldRefAccess<SplitDialog, TMP_Text>("m_splitAmount");
            _splitOk = AccessTools.FieldRefAccess<SplitDialog, Button>("m_splitOkButton");
            _splitCancel = AccessTools.FieldRefAccess<SplitDialog, Button>("m_splitCancelButton");
            _variantElements = AccessTools.FieldRefAccess<VariantDialog, List<GameObject>>("m_elements");
            foreach (var d in All()) { d.Skin = new VanillaSkin(Owner + ":" + d.Name); d.Shown = false; }
        }

        public void Refresh(float deltaSeconds)
        {
            var gui = InventoryGui.instance;
            UpdateSplit(gui);
            UpdateVariant(gui);
            UpdateReaders();
            UpdateInput();
        }

        public void Teardown()
        {
            foreach (var d in All())
            {
                if (d.Shown) d.Skin.Restore();
                d.Shown = false;
                if (d.Root != null) Object.Destroy(d.Root.gameObject);
                d.Root = d.Board = null;
            }
            _variantCells.Clear();
            _variantCount = -1;
            _shownReader = _shownRaven = null;
        }

        private IEnumerable<Dialog> All()
        {
            yield return _split;
            yield return _variant;
            yield return _reader;
            yield return _raven;
            yield return _input;
        }

        /// <summary>Shows or hides one mirrored dialog; veils vanilla's visible part while it shows.</summary>
        private bool Show(Dialog d, bool open, Component anchor, GameObject vanillaVisual, bool keepInteractable, System.Action<RectTransform> draw)
        {
            if (!open)
            {
                if (d.Shown)
                {
                    d.Shown = false;
                    d.Skin.Restore();
                    if (d.Root != null) d.Root.gameObject.SetActive(false);
                }
                return false;
            }
            if (d.Root == null)
            {
                d.Root = WindowCanvas.CreateRoot(anchor, "GenesisUI.Dialog." + d.Name, behind: false);
                if (d.Root == null) return false;
                d.Board = WindowCanvas.Area(d.Root, "Board");
                draw(d.Board);
            }
            if (!d.Shown)
            {
                d.Shown = true;
                if (vanillaVisual != null)
                {
                    d.Skin.Hidden(vanillaVisual, keepInteractable);
                }
                d.Root.gameObject.SetActive(true);
                d.Root.SetAsLastSibling(); // above GenesisUI's windows on the same canvas
            }
            WindowCanvas.Fit(d.Board);
            return true;
        }

        // ------------------------------------------------------------------ split

        private void UpdateSplit(InventoryGui gui)
        {
            var dialog = gui != null ? gui.m_splitDialog : null;
            bool open = dialog != null && dialog.IsActive;
            var panel = open ? _splitPanel(dialog) : null;
            if (!Show(_split, open, gui, panel != null ? panel.gameObject : null, true, DrawSplit)) return;
            var icon = _splitIconRef(dialog);
            if (icon != null && _splitIcon.sprite != icon.sprite) _splitIcon.sprite = icon.sprite;
            var name = _splitNameRef(dialog);
            if (name != null && _splitName.text != name.text) _splitName.text = name.text;
            var amount = _splitAmountRef(dialog);
            if (amount != null && _splitAmount.text != amount.text) _splitAmount.text = amount.text;
            var slider = dialog.m_splitSlider;
            _syncingSlider = true;
            if (_splitSlider.minValue != slider.minValue) _splitSlider.minValue = slider.minValue;
            if (_splitSlider.maxValue != slider.maxValue) _splitSlider.maxValue = slider.maxValue;
            _splitSlider.wholeNumbers = slider.wholeNumbers;
            if (!Mathf.Approximately(_splitSlider.value, slider.value)) _splitSlider.SetValueWithoutNotify(slider.value);
            _syncingSlider = false;
        }

        private void DrawSplit(RectTransform board)
        {
            var t = _theme.Tokens;
            const float w = 460f, h = 250f;
            var card = WindowCanvas.At(board, "Card", (WindowCanvas.Design.x - w) / 2f, (WindowCanvas.Design.y - h) / 2f, w, h);
            Ui.Image(card, null, new Color(0f, 0f, 0f, 0f), raycast: true);
            Frame.Dress(card, _theme, "window_panel", "Windows");
            var title = _parts.Label(card, "Title", FontRole.Display, 19f, t.AccentGoldBright, 40f, 14f, w - 80f, 30f, TextAlignmentOptions.Center);
            title.text = Localize("$genesisui_split_title").ToUpperInvariant();
            title.characterSpacing = 4f;
            _splitIcon = Ui.Image(WindowCanvas.At(card, "Icon", 36f, 60f, 64f, 64f), null, Color.white);
            _splitIcon.preserveAspect = true;
            _splitName = _parts.Label(card, "Name", FontRole.Body, 18f, t.TextTitle, 114f, 62f, w - 150f, 26f, TextAlignmentOptions.Left);
            _splitAmount = _parts.Label(card, "Amount", FontRole.Display, 26f, t.AccentGoldBright, 114f, 90f, w - 150f, 34f, TextAlignmentOptions.Left);
            _splitSlider = MakeSlider(card, 36f, 140f, w - 72f);
            _splitSlider.onValueChanged.AddListener(v => Guard.Try("split slider", () =>
            {
                if (_syncingSlider) return;
                var gui = InventoryGui.instance;
                if (gui != null && gui.m_splitDialog != null) gui.m_splitDialog.m_splitSlider.value = v; // vanilla updates its amount
            }));
            _parts.Button(card, "Cancel", 36f, 186f, 180f, 42f, "$genesisui_cancel", 18f, "split cancel", () => Press(_splitCancel), out _);
            _parts.Button(card, "Ok", w - 36f - 180f, 186f, 180f, 42f, "$genesisui_ok", 18f, "split ok", () => Press(_splitOk), out var ok);
            ok.font = _theme.Font(FontRole.Display);
        }

        private void Press(AccessTools.FieldRef<SplitDialog, Button> button)
        {
            var gui = InventoryGui.instance;
            if (gui == null || gui.m_splitDialog == null) return;
            var b = button(gui.m_splitDialog);
            if (b != null) b.onClick.Invoke();
        }

        private Slider MakeSlider(RectTransform parent, float x, float y, float width)
        {
            var t = _theme.Tokens;
            var rt = WindowCanvas.At(parent, "Slider", x, y, width, 24f);
            Ui.Image(WindowCanvas.At(rt, "Track", 0f, 10f, width, 4f), null, new Color(0f, 0f, 0f, 0.6f));
            var fillArea = WindowCanvas.At(rt, "FillArea", 0f, 10f, width, 4f);
            var fill = Ui.Fill(Ui.Child(fillArea, "Fill"));
            Ui.Image(fill, _theme.Sprite("bar_fill"), ThemeRuntime.ToUnity(t.AccentGold));
            var handleArea = WindowCanvas.At(rt, "HandleArea", 10f, 0f, width - 20f, 24f);
            var handle = Ui.Child(handleArea, "Handle");
            handle.sizeDelta = new Vector2(20f, 24f);
            Frame.Dress(handle, _theme, "keycap", "Windows", 24f);
            var hit = Ui.Image(rt, null, new Color(0f, 0f, 0f, 0f), raycast: true);
            var slider = rt.gameObject.AddComponent<Slider>();
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = hit;
            slider.transition = Selectable.Transition.None;
            return slider;
        }

        // ------------------------------------------------------------------ variant (style)

        private void UpdateVariant(InventoryGui gui)
        {
            var dialog = gui != null ? gui.m_variantDialog : null;
            bool open = dialog != null && dialog.gameObject.activeInHierarchy;
            if (!Show(_variant, open, gui, open ? dialog.gameObject : null, false, DrawVariant)) { _variantCount = -1; return; }
            var elements = _variantElements(dialog);
            var first = elements != null && elements.Count > 0 ? elements[0] : null;
            if (elements == null || (elements.Count == _variantCount && first == _variantFirst)) return;
            _variantCount = elements.Count;
            _variantFirst = first;
            for (int i = 0; i < _variantCells.Count; i++)
            {
                bool show = i < elements.Count && elements[i] != null;
                _variantCells[i].Root.gameObject.SetActive(show);
                if (!show) continue;
                // The element's icon: the first image with a sprite below the element.
                Sprite sprite = null;
                foreach (var img in elements[i].GetComponentsInChildren<Image>(true))
                    if (img.sprite != null && img.gameObject != elements[i]) { sprite = img.sprite; if (img.name.ToLowerInvariant().Contains("icon")) break; }
                _variantCells[i].Icon.sprite = sprite;
            }
        }

        private void DrawVariant(RectTransform board)
        {
            var t = _theme.Tokens;
            const float w = 640f, h = 420f, cell = 88f, pitch = 98f;
            var card = WindowCanvas.At(board, "Card", (WindowCanvas.Design.x - w) / 2f, (WindowCanvas.Design.y - h) / 2f, w, h);
            Ui.Image(card, null, new Color(0f, 0f, 0f, 0f), raycast: true);
            Frame.Dress(card, _theme, "window_panel", "Windows");
            var title = _parts.Label(card, "Title", FontRole.Display, 19f, t.AccentGoldBright, 40f, 14f, w - 80f, 30f, TextAlignmentOptions.Center);
            title.text = Localize("$genesisui_style_title").ToUpperInvariant();
            title.characterSpacing = 4f;
            for (int i = 0; i < 18; i++)
            {
                int index = i;
                var root = WindowCanvas.At(card, "Style " + i, 30f + (i % 6) * pitch, 64f + (i / 6) * pitch, cell, cell);
                Frame.Dress(root, _theme, "hotslot", "Windows", cell);
                var icon = Ui.Image(WindowCanvas.At(root, "Icon", 10f, 10f, cell - 20f, cell - 20f), null, Color.white);
                icon.preserveAspect = true;
                _parts.Clickable(root, "style pick", () => PickVariant(index));
                root.gameObject.SetActive(false);
                _variantCells.Add((root, icon));
            }
            _parts.Button(card, "Close", (w - 200f) / 2f, h - 64f, 200f, 42f, "$genesisui_hint_close", 18f, "style close",
                () => { var gui = InventoryGui.instance; if (gui != null) gui.m_variantDialog.OnClose(); }, out _);
        }

        private void PickVariant(int index)
        {
            var gui = InventoryGui.instance;
            if (gui == null) return;
            var elements = _variantElements(gui.m_variantDialog);
            if (elements == null || index >= elements.Count || elements[index] == null) return;
            var button = elements[index].GetComponentInChildren<Button>();
            if (button != null) button.onClick.Invoke();
        }

        // ------------------------------------------------------------------ rune stones and ravens

        private void UpdateReaders()
        {
            var tv = TextViewer.instance;
            bool visible = tv != null && tv.IsVisible();
            bool normal = visible && tv.m_root != null && tv.m_root.activeInHierarchy;
            bool raven = visible && tv.m_ravenRoot != null && tv.m_ravenRoot.activeInHierarchy;
            if (Show(_reader, normal, tv, normal ? tv.m_root : null, true, DrawReader))
            {
                string key = (tv.m_topic != null ? tv.m_topic.text : "") + "\n" + (tv.m_text != null ? tv.m_text.text : "");
                if (key != _shownReader)
                {
                    _shownReader = key;
                    _readerTopic.text = tv.m_topic != null ? tv.m_topic.text.ToUpperInvariant() : "";
                    _readerText.text = tv.m_text != null ? tv.m_text.text : "";
                }
            }
            if (Show(_raven, raven, tv, raven ? tv.m_ravenRoot : null, true, DrawRaven))
            {
                string key = (tv.m_ravenTopic != null ? tv.m_ravenTopic.text : "") + "\n" + (tv.m_ravenText != null ? tv.m_ravenText.text : "");
                if (key != _shownRaven)
                {
                    _shownRaven = key;
                    _ravenTopic.text = tv.m_ravenTopic != null ? tv.m_ravenTopic.text.ToUpperInvariant() : "";
                    _ravenText.text = tv.m_ravenText != null ? tv.m_ravenText.text : "";
                }
            }
        }

        private void DrawReader(RectTransform board)
        {
            var t = _theme.Tokens;
            const float w = 760f, h = 470f;
            var card = WindowCanvas.At(board, "Card", (WindowCanvas.Design.x - w) / 2f, (WindowCanvas.Design.y - h) / 2f - 40f, w, h);
            Frame.Dress(card, _theme, "window_panel", "Windows");
            _readerTopic = _parts.Label(card, "Topic", FontRole.Display, 22f, t.AccentGoldBright, 50f, 16f, w - 100f, 32f, TextAlignmentOptions.Center);
            _readerTopic.characterSpacing = 4f;
            _parts.Rule(card, w * 0.2f, 56f, w * 0.6f);
            _readerText = _parts.Label(card, "Text", FontRole.Body, 19f, t.TextBody, 50f, 76f, w - 100f, h - 130f, TextAlignmentOptions.Top);
            _readerText.textWrappingMode = TextWrappingModes.Normal;
            _readerText.enableAutoSizing = true;
            _readerText.fontSizeMin = 13f;
            var hint = _parts.Label(card, "Hint", FontRole.Body, 15f, t.TextFlavor, 50f, h - 44f, w - 100f, 24f, TextAlignmentOptions.Center);
            hint.text = Localize("$genesisui_reader_close");
        }

        private void DrawRaven(RectTransform board)
        {
            var t = _theme.Tokens;
            const float w = 560f, h = 300f;
            var card = WindowCanvas.At(board, "Card", WindowCanvas.Design.x - w - 20f, 150f, w, h);
            Frame.Dress(card, _theme, "window_panel", "Windows");
            _ravenTopic = _parts.Label(card, "Topic", FontRole.Display, 20f, t.AccentGoldBright, 40f, 16f, w - 80f, 30f, TextAlignmentOptions.Left);
            _ravenTopic.characterSpacing = 3f;
            _parts.Rule(card, 34f, 54f, 260f);
            _ravenText = _parts.Label(card, "Text", FontRole.Body, 17f, t.TextBody, 40f, 72f, w - 80f, h - 110f, TextAlignmentOptions.TopLeft);
            _ravenText.textWrappingMode = TextWrappingModes.Normal;
            _ravenText.enableAutoSizing = true;
            _ravenText.fontSizeMin = 12f;
            var hint = _parts.Label(card, "Hint", FontRole.Body, 14f, t.TextFlavor, 40f, h - 36f, w - 80f, 22f, TextAlignmentOptions.Right);
            hint.text = Localize("$genesisui_reader_close");
        }

        // ------------------------------------------------------------------ naming (signs, portals, tames)

        private void UpdateInput()
        {
            var input = TextInput.instance;
            bool open = input != null && TextInput.IsVisible() && input.m_panel != null && input.m_panel.activeInHierarchy;
            if (!Show(_input, open, input, open ? input.m_panel : null, true, DrawInput)) return;
            string topic = input.m_topic != null ? input.m_topic.text : "";
            if (_inputTopic.text != topic) _inputTopic.text = topic.ToUpperInvariant();
            var field = (TMP_InputField)(object)input.m_inputField;
            string text = field != null ? field.text : "";
            string shown = text + (field != null && field.isFocused && Mathf.Repeat(Time.unscaledTime, 1f) < 0.5f ? "|" : "");
            if (_inputText.text != shown) _inputText.text = shown;
        }

        private void DrawInput(RectTransform board)
        {
            var t = _theme.Tokens;
            const float w = 620f, h = 230f;
            var card = WindowCanvas.At(board, "Card", (WindowCanvas.Design.x - w) / 2f, (WindowCanvas.Design.y - h) / 2f, w, h);
            Ui.Image(card, null, new Color(0f, 0f, 0f, 0f), raycast: true);
            Frame.Dress(card, _theme, "window_panel", "Windows");
            _inputTopic = _parts.Label(card, "Topic", FontRole.Display, 19f, t.AccentGoldBright, 40f, 14f, w - 80f, 30f, TextAlignmentOptions.Center);
            _inputTopic.characterSpacing = 4f;
            var box = WindowCanvas.At(card, "Box", 36f, 64f, w - 72f, 46f);
            Frame.Dress(box, _theme, "keycap_wide", "Windows", 46f);
            _inputText = _parts.Label(box, "Text", FontRole.Body, 20f, t.TextTitle, 16f, 0f, w - 104f, 46f, TextAlignmentOptions.MidlineLeft);
            _parts.Clickable(box, "input focus", FocusInput);
            _parts.Button(card, "Cancel", 36f, h - 74f, 200f, 44f, "$genesisui_cancel", 18f, "input cancel",
                () => { var i = TextInput.instance; if (i != null) i.OnCancel(); }, out _);
            _parts.Button(card, "Ok", w - 36f - 200f, h - 74f, 200f, 44f, "$genesisui_ok", 18f, "input ok",
                () => { var i = TextInput.instance; if (i != null) i.OnEnter(); }, out var ok);
            ok.font = _theme.Font(FontRole.Display);
        }

        /// <summary>Typing goes to vanilla's own field, which vanilla focuses when it opens.</summary>
        private void FocusInput()
        {
            var input = TextInput.instance;
            var field = input != null ? (TMP_InputField)(object)input.m_inputField : null;
            if (field == null) return;
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(field.gameObject);
            field.ActivateInputField();
        }

        private static string Localize(string text) => WindowParts.Localize(text);
    }
}
