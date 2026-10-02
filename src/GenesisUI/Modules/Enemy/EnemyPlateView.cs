using GenesisUI.Theme;
using GenesisUI.Widgets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GenesisUI.Modules.Enemy
{
    /// <summary>
    /// One creature plate, living inside the vanilla plate it replaces. Reads what vanilla shows
    /// (name, health bars, level stars, alerted / aware marks) from the vanilla plate's own
    /// children and draws it in our style. Allocation-free per frame; a missing vanilla child
    /// just leaves that detail out.
    /// </summary>
    [GenesisUI.Foundation.Contracts.GameContract("assembly_guiutils", "GuiBar", "GetSmoothValue", Parameters = new string[] {  }, Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Method, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Single")]
    [GenesisUI.Foundation.Contracts.ContractDependency(typeof(GenesisUI.Theme.ThemeRuntime), typeof(GenesisUI.Theme.ThemeTokens), typeof(GenesisUI.Widgets.Ui), typeof(GenesisUI.Widgets.LiquidLayer))]
    internal sealed class EnemyPlateView
    {
        private const float Width = 112f;
        private const float PlateHeight = 18f;
        private const float PlateScale = 0.3f;          // the boss plate's ornament, drawn small
        private const float NameGap = 1f;

        private readonly RectTransform _root;
        private readonly Image _fill;
        private readonly Image _trail;
        private readonly RectTransform _liquidMask;
        private readonly LiquidLayer _liquid;
        private readonly Image[] _stars = new Image[2];
        private readonly TextMeshProUGUI _name;
        private readonly TextMeshProUGUI _mark;
        private readonly Color _hostile;
        private readonly Color _friendly;
        private readonly Color _danger;
        private readonly Color _aware;

        private readonly TextMeshProUGUI _vanillaName;
        private readonly GuiBar _vanillaFast;
        private readonly GuiBar _vanillaFriendly;
        private readonly GuiBar _vanillaSlow;
        private readonly GameObject _level2;
        private readonly GameObject _level3;
        private readonly GameObject _alerted;
        private readonly GameObject _awareMark;

        private string _shownName;
        private int _shownStars = -1;
        private int _shownMark = -1;
        private int _shownOffset = int.MinValue;
        private bool _shownFriendly;

        public EnemyPlateView(Transform vanilla, ThemeRuntime theme)
        {
            var t = theme.Tokens;
            _hostile = ThemeRuntime.ToUnity(t.BarHealth);
            _friendly = ThemeRuntime.ToUnity(t.StatePositive);
            _danger = ThemeRuntime.ToUnity(t.StateDanger);
            _aware = ThemeRuntime.ToUnity(t.AccentGold);

            _vanillaName = Find<TextMeshProUGUI>(vanilla, "Name");
            _vanillaFast = Find<GuiBar>(vanilla, "Health/health_fast");
            _vanillaFriendly = Find<GuiBar>(vanilla, "Health/health_fast_friendly");
            _vanillaSlow = Find<GuiBar>(vanilla, "Health/health_slow");
            _level2 = FindObject(vanilla, "level_2");
            _level3 = FindObject(vanilla, "level_3");
            _alerted = FindObject(vanilla, "Alerted");
            _awareMark = FindObject(vanilla, "Aware");

            // Our plate is a child of vanilla's: it follows its position and visibility, and
            // its own CanvasGroup ignores the veil vanilla's plate is under.
            _root = Ui.Child(vanilla, "GenesisUI.EnemyPlate");
            _root.anchorMin = _root.anchorMax = new Vector2(0.5f, 0.5f);
            _root.pivot = new Vector2(0.5f, 0f);
            _root.sizeDelta = new Vector2(Width, PlateHeight + 20f);
            var group = _root.gameObject.AddComponent<CanvasGroup>();
            group.ignoreParentGroups = true;
            group.blocksRaycasts = false;
            group.interactable = false;

            var plateRt = Ui.Place(Ui.Child(_root, "Plate"), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(Width, PlateHeight));
            // The generated boss plate drawn small, as approved in F3 (restored after R-042).
            string plateName = theme.Sprite("enemy_plate") != null ? "enemy_plate" : "boss_plate";
            var plateSprite = theme.Sprite(plateName);
            var plate = Ui.Image(plateRt, plateSprite, plateSprite != null ? Color.white : ThemeRuntime.ToUnity(t.PanelBackground));
            plate.pixelsPerUnitMultiplier = 1f / PlateScale;

            var c = theme.Content(plateName, new Vector4(22f, 12f, 22f, 12f)) * PlateScale;
            // The bar fills the plate's whole inner channel (R-048: black corners showed around it) and
            // carries Diego's health liquid like every health bar; tamed creatures keep a green fill.
            var bar = Ui.Fill(Ui.Child(plateRt, "Bar"), c.x - 1f, c.y, c.z - 1f, c.w);
            var fillSprite = theme.Sprite("bar_fill");
            _trail = Filled(Ui.Image(Ui.Fill(Ui.Child(bar, "Trail")), fillSprite, new Color(1f, 0.62f, 0.26f, 0.35f)));
            _fill = Filled(Ui.Image(Ui.Fill(Ui.Child(bar, "Fill")), fillSprite, _hostile));
            var liquid = theme.Texture("liquid_health_h");
            if (liquid != null)
            {
                _liquidMask = Ui.Child(bar, "Liquid");
                _liquidMask.anchorMin = Vector2.zero;
                _liquidMask.anchorMax = Vector2.one;
                _liquidMask.offsetMin = _liquidMask.offsetMax = Vector2.zero;
                _liquidMask.gameObject.AddComponent<RectMask2D>();
                var size = new Vector2(Width - c.x - c.z + 2f, PlateHeight - c.y - c.w);
                var flow = Ui.Place(Ui.Child(_liquidMask, "Flow"), new Vector2(0f, 0.5f), Vector2.zero, size);
                flow.pivot = new Vector2(0f, 0.5f);
                _liquid = new LiquidLayer(flow, liquid, size, theme.Size("liquid_health_h"), 1.6f, Color.white,
                    new Vector2(0.015f, 0f), new Vector2(0f, 0.3f), mirror: false);
                _fill.enabled = false;
            }

            var star = theme.Sprite("star");
            for (int i = 0; i < _stars.Length; i++)
            {
                var srt = Ui.Place(Ui.Child(_root, "Star" + i), new Vector2(0.5f, 0f), new Vector2(0f, PlateHeight), new Vector2(10f, 10f));
                srt.pivot = new Vector2(0.5f, 0.5f);
                _stars[i] = Ui.Image(srt, star, star != null ? Color.white : ThemeRuntime.ToUnity(t.AccentGold));
                _stars[i].enabled = false;
            }

            _name = Ui.Fit(Ui.Text(_root, "Name", theme, FontRole.Label, 14f, ThemeRuntime.ToUnity(t.TextTitle),
                                   TextAlignmentOptions.Bottom, outlined: true), 10f);
            var nameRt = (RectTransform)_name.transform;
            nameRt.anchorMin = new Vector2(0f, 0f);
            nameRt.anchorMax = new Vector2(1f, 1f);
            nameRt.offsetMin = new Vector2(0f, PlateHeight + NameGap + 4f);
            nameRt.offsetMax = new Vector2(0f, 4f);

            _mark = Ui.Text(_root, "Mark", theme, FontRole.Display, 16f, _danger, TextAlignmentOptions.Center, outlined: true);
            var markRt = (RectTransform)_mark.transform;
            markRt.anchorMin = markRt.anchorMax = new Vector2(0f, 0f);
            markRt.pivot = new Vector2(0.5f, 0.5f);
            markRt.anchoredPosition = new Vector2(-8f, PlateHeight / 2f);
            markRt.sizeDelta = new Vector2(14f, 18f);
            _mark.enabled = false;
        }

        public void Apply(int offsetY)
        {
            if (_root == null) return;
            if (offsetY != _shownOffset)
            {
                _shownOffset = offsetY;
                _root.anchoredPosition = new Vector2(0f, offsetY);
            }

            if (_vanillaName != null && !string.Equals(_vanillaName.text, _shownName))
            {
                _shownName = _vanillaName.text;
                _name.text = _shownName;
            }

            bool friendly = _vanillaFriendly != null && _vanillaFriendly.gameObject.activeSelf;
            var fast = friendly ? _vanillaFriendly : _vanillaFast;
            if (fast != null) _fill.fillAmount = Mathf.Clamp01(fast.GetSmoothValue());
            if (_liquid != null)
            {
                _fill.enabled = friendly;
                if (_liquidMask.gameObject.activeSelf == friendly) _liquidMask.gameObject.SetActive(!friendly);
                if (!friendly)
                {
                    if (!Mathf.Approximately(_liquidMask.anchorMax.x, _fill.fillAmount)) _liquidMask.anchorMax = new Vector2(_fill.fillAmount, 1f);
                    _liquid.Scroll(Time.unscaledDeltaTime);
                }
            }
            if (_vanillaSlow != null) _trail.fillAmount = Mathf.Max(_fill.fillAmount, Mathf.Clamp01(_vanillaSlow.GetSmoothValue()));
            if (friendly != _shownFriendly)
            {
                _shownFriendly = friendly;
                _fill.color = friendly ? _friendly : _hostile;
            }

            int stars = _level3 != null && _level3.activeSelf ? 2 : _level2 != null && _level2.activeSelf ? 1 : 0;
            if (stars != _shownStars)
            {
                _shownStars = stars;
                for (int i = 0; i < _stars.Length; i++)
                {
                    _stars[i].enabled = i < stars;
                    _stars[i].rectTransform.anchoredPosition = new Vector2((i - (stars - 1) / 2f) * 11f, PlateHeight);
                }
            }

            // 0 none, 1 aware of a target, 2 alerted (vanilla's own marks).
            int mark = _alerted != null && _alerted.activeSelf ? 2 : _awareMark != null && _awareMark.activeSelf ? 1 : 0;
            if (mark != _shownMark)
            {
                _shownMark = mark;
                _mark.enabled = mark != 0;
                if (mark == 2) { _mark.text = "!"; _mark.color = _danger; }
                else if (mark == 1) { _mark.text = "?"; _mark.color = _aware; }
            }
        }

        public void Destroy()
        {
            if (_root != null) Object.Destroy(_root.gameObject);
        }

        private static Image Filled(Image image)
        {
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillOrigin = (int)Image.OriginHorizontal.Left;
            image.fillAmount = 0f;
            return image;
        }

        private static T Find<T>(Transform root, string path) where T : Component
        {
            var t = root.Find(path);
            return t != null ? t.GetComponent<T>() : null;
        }

        private static GameObject FindObject(Transform root, string path)
        {
            var t = root.Find(path);
            return t != null ? t.gameObject : null;
        }
    }
}
