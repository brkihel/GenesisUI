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
    [GameContract("assembly_guiutils", "UITooltip", "m_tooltip", Kind = ContractMemberKind.Field, Static = ContractStatic.Static, ValueType = "UnityEngine.GameObject")]
    [ContractDependency(typeof(VanillaSkin), typeof(ThemeRuntime), typeof(Ui))]
    internal sealed class ItemTooltipModule : IUiModule
    {
        public string Id => "win.tooltips";
        public string NameToken => "$genesisui_module_item_tooltips";
        public IReadOnlyList<string> Regions => new string[0];
        public float RefreshRate => 0f;
        private readonly VanillaSkin _skin = new VanillaSkin("module:win.tooltips");
        private readonly List<TMP_Text> _texts = new List<TMP_Text>(32);
        private System.Reflection.FieldInfo _native;
        private ThemeRuntime _theme;
        private GameObject _tip, _border;
        private Image _background;
        public void Build(ModuleContext context)
        {
            _theme = context.Theme;
            _native = AccessTools.Field(typeof(UITooltip), "m_tooltip");
        }
        public void Refresh(float deltaSeconds)
        {
            var tip = WindowShellModule.Showing ? _native.GetValue(null) as GameObject : null;
            if (tip != _tip)
            {
                Restore(); _tip = tip;
                if (tip == null) return;
                var background = tip.transform.Find("Bkg");
                if (background == null) return;
                _background = _skin.Image(background.GetComponent<Image>());
                _border = Ui.Fill(Ui.Child(background, "GenesisUI.TooltipBorder")).gameObject;
                Border(0f, 0f, 1f, 0f, 0f, 1f);
                Border(0f, 1f, 1f, 1f, 0f, 1f);
                Border(0f, 0f, 0f, 1f, 1f, 0f);
                Border(1f, 0f, 1f, 1f, 1f, 0f);
                GenesisLog.Info("Module:win.tooltips", "native tooltip styled; foreign content retained");
            }
            if (tip == null) return;
            if (_background != null)
            {
                if (_background.sprite != null) _background.sprite = null;
                if (_background.type != Image.Type.Simple) _background.type = Image.Type.Simple;
                var color = ThemeRuntime.ToUnity(_theme.Tokens.PanelBackground);
                if (_background.color != color) _background.color = color;
            }
            tip.GetComponentsInChildren(true, _texts);
            foreach (var text in _texts)
            {
                _skin.Text(text);
                var font = _theme.Font(FontRole.Body);
                if (font != null && text.font != font) { text.font = font; text.fontSharedMaterial = font.material; }
            }
        }
        private void Border(float x0, float y0, float x1, float y1, float width, float height)
        {
            var rt = Ui.Fill(Ui.Child(_border.transform, "Line"));
            rt.anchorMin = new Vector2(x0, y0); rt.anchorMax = new Vector2(x1, y1);
            rt.sizeDelta = new Vector2(width, height);
            Ui.Image(rt, null, ThemeRuntime.ToUnity(_theme.Tokens.LineFrame), raycast: false);
        }
        private void Restore()
        {
            _skin.Restore();
            if (_border != null) Object.Destroy(_border);
            _border = _tip = null; _background = null; _texts.Clear();
        }
        public void Teardown() => Restore();
    }
}
