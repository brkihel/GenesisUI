using GenesisUI.Theme;
using GenesisUI.Vitals;
using GenesisUI.Widgets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GenesisUI.Modules.Vitals
{
    /// <summary>How a bar's living liquid moves and burns (docs/ART-DIRECTION.md §5: quiet motion).</summary>
    internal struct BarMotion
    {
        /// <summary>Upward drift of the veins, in tile heights per second (clots and bubbles derive from it).</summary>
        public float Speed;
        /// <summary>Opacity of the bubbles: a subtle detail (Diego, R-030).</summary>
        public float PatternAlpha;
        /// <summary>A second, slower bubble layer for parallax (eitr); 0 = none.</summary>
        public float CounterSpeed;
        /// <summary>Colour of the burn and its embers where the bar is consumed.</summary>
        public Color Hot;
        /// <summary>The bar's frame (art/sprites.json) and its liquid texture (tools/art/sheets.py).</summary>
        public string Frame;
        public string Liquid;
    }

    /// <summary>
    /// One vertical vital bar, v4 (F4.0, D-027: frame and liquid from Diego's texture sheets).
    ///
    /// The frame is drawn at its own size, never stretched; the panel material sits behind it,
    /// clipped to the frame's silhouette, with its own opacity. The liquid is Diego's texture in
    /// two drifting layers, clipped to the frame's opening and to the level.
    ///
    /// Earlier, v3 (R-032 references: internal blood texture, burn when consumed;
    /// R-040 comparison: calm liquid, compact value plate).
    ///
    /// Inside the frame's content area (declared in art/sprites.json): a liquid in a mask that
    /// rises and falls, made of the tinted body, dark clots, bright veins and faint bubbles,
    /// each drifting up at its own pace. Above the liquid, the part just lost burns: a hot band
    /// at the surface breaking into grains, with embers rising. The number sits on the value
    /// plate when the art has one. Low health pulses the frame red, with embers along the sides.
    /// Only draws what a BarAnimator says; allocation-free per frame.
    /// </summary>
    [GenesisUI.Foundation.Contracts.ContractDependency(typeof(GenesisUI.Widgets.Ui), typeof(GenesisUI.Theme.ThemeRuntime), typeof(GenesisUI.Widgets.Frame), typeof(GenesisUI.Theme.ThemeTokens), typeof(GenesisUI.Widgets.LiquidLayer), typeof(GenesisUI.Widgets.BurnLight), typeof(GenesisUI.Vitals.BarAnimator))]
    internal sealed class VitalBarView
    {
        private const int EmberCount = 16;
        private const float MaxBurnHeight = 9f;         // the burn is a short bright edge, never a long trail (R-040)
        private const float PlateAt = 0.26f;            // plate centre, as a fraction of the liquid's height
        private const float PlateWidth = 0.92f;         // plate width, as a fraction of the bar's width
        private const float LiquidDensity = 1.4f;       // texture units per bar unit: the texture's detail, a little finer
        private const float LiquidPace = 0.45f;         // the texture drifts slower than the old patterns: it is richer

        public readonly RectTransform Root;
        private readonly Image _frame;
        private readonly RectTransform _mask;
        private readonly RectTransform _burnMask;
        private readonly RawImage _burn;
        private readonly RawImage _mottle;
        private readonly RawImage _veins;
        private readonly LiquidLayer _liquid;
        private readonly LiquidLayer _liquidShimmer;
        private readonly RawImage _bubbles;
        private readonly RawImage _counterBubbles;
        private readonly Image _surface;
        private readonly Image _glint;
        private readonly RectTransform _glintRt;
        private readonly TextMeshProUGUI _number;
        private readonly GameObject _valueRoot;
        private readonly Color _frameColor;
        private readonly Color _dangerColor;
        private readonly Color _light;
        private readonly Vector2 _size;
        private readonly float _areaHeight;
        private readonly float _areaWidth;
        private readonly float _areaBottom;
        private readonly float _areaLeft;
        private readonly float _tileHeightUv;
        private readonly float _glintTravel;
        private readonly BarMotion _motion;

        private readonly Ember[] _embers = new Ember[EmberCount];
        private readonly System.Random _random;
        private float _emitBurn;
        private float _emitSides;
        private float _shownFast = -1f;
        private float _lastFast = -1f;
        private float _activity;
        private float _time;

        private struct Ember
        {
            public RectTransform Rt;
            public Image Image;
            public Vector2 Position;
            public Vector2 Velocity;
            public float Life;
            public float MaxLife;
        }

        public VitalBarView(RectTransform parent, string name, ThemeRuntime theme, ColorRgba barColor, Vector2 position, Vector2 size, BarMotion motion)
        {
            _motion = motion;
            _size = size;
            _random = new System.Random(name.GetHashCode());
            Root = Ui.Place(Ui.Child(parent, name), new Vector2(0f, 0f), position, size);

            // The frame of this bar's size when the art has it; else the shared generic frame.
            string frameName = motion.Frame != null && theme.Sprite(motion.Frame) != null ? motion.Frame : "bar_frame";
            Widgets.Frame.Background(Root, theme, frameName, "Vitals");
            var frameSprite = theme.Sprite(frameName);
            _frameColor = frameSprite != null ? Color.white : ThemeRuntime.ToUnity(theme.Tokens.PanelBackground);
            _dangerColor = ThemeRuntime.ToUnity(theme.Tokens.StateDanger);
            _frame = Ui.Image(Ui.Fill(Ui.Child(Root, "Frame")), frameSprite, _frameColor);

            // The content area comes from art/sprites.json; the fallback matches the frame drawn today.
            var c = theme.Content(frameName, new Vector4(10f, 24f, 10f, 32f));
            _areaLeft = c.x;
            _areaBottom = c.y;
            _areaWidth = size.x - c.x - c.z;
            _areaHeight = size.y - c.y - c.w;

            // When the art provides the shape of the frame's inner opening (same size and 9-slice
            // as the frame), the liquid and its effects are clipped by that shape: they reach into
            // the arch and the point, with no black corners (Diego, F4.0). Otherwise a rectangle.
            Transform areaParent = Root;
            var openingSprite = theme.Sprite(frameName + "_opening");
            if (openingSprite != null)
            {
                var opening = Ui.Fill(Ui.Child(Root, "Opening"));
                Ui.Image(opening, openingSprite, Color.white);
                opening.gameObject.AddComponent<Mask>().showMaskGraphic = false;
                areaParent = opening;
                // The frame's opening is transparent in this art: draw the frame over the liquid,
                // so its inner rim overlaps the liquid's edge.
                _frame.transform.SetAsLastSibling();
            }
            var area = Ui.Fill(Ui.Child(areaParent, "FillArea"), c.x, c.y, c.z, c.w);

            var color = ThemeRuntime.ToUnity(barColor);
            _light = Color.Lerp(color, Color.white, 0.4f);
            var fillSprite = theme.Sprite("bar_fill");

            // The consumed part, just above the liquid: a burn that the mask keeps to that span.
            _burnMask = Ui.Child(area, "Burn");
            Bottom(_burnMask, 0f, 0f);
            _burnMask.gameObject.AddComponent<RectMask2D>();
            var burnTex = theme.Texture("bar_burn");
            if (burnTex != null)
            {
                var brt = Ui.Fill(Ui.Child(_burnMask, "Grains"));
                _burn = brt.gameObject.AddComponent<RawImage>();
                _burn.texture = burnTex;
                _burn.color = motion.Hot;
                _burn.raycastTarget = false;
                _burn.uvRect = new Rect(0f, 0f, 2f, 1f);
            }

            // The liquid: its layers keep the full height; only the mask moves.
            _mask = Ui.Child(area, "Liquid");
            Bottom(_mask, 0f, 0f);
            _mask.gameObject.AddComponent<RectMask2D>();
            _tileHeightUv = _areaWidth > 0f ? _areaHeight / (2f * _areaWidth) : 1f;
            var liquidTexture = motion.Liquid != null ? theme.Texture(motion.Liquid) : null;
            if (liquidTexture != null)
            {
                // Diego's liquid: a slow body and a mirrored, fainter layer drifting at another pace,
                // so the surface seems to move inside without ever repeating in step.
                var area2 = new Vector2(_areaWidth, _areaHeight);
                var texSize = theme.Size(motion.Liquid);
                float across = Mathf.Max(0f, 1f - _areaWidth * LiquidDensity / Mathf.Max(1f, texSize.x));
                _liquid = new LiquidLayer(FullHeight(Ui.Child(_mask, "Liquid")), liquidTexture, area2, texSize, LiquidDensity,
                    Color.white, new Vector2(0f, motion.Speed * LiquidPace), new Vector2(across * 0.5f, 0f), mirror: false);
                _liquidShimmer = new LiquidLayer(FullHeight(Ui.Child(_mask, "Shimmer")), liquidTexture, area2, texSize, LiquidDensity * 0.8f,
                    new Color(1f, 1f, 1f, 0.35f), new Vector2(0f, motion.Speed * LiquidPace * 0.55f), new Vector2(across * 0.2f, 0.43f), mirror: true);
            }
            else
            {
                Ui.Image(FullHeight(Ui.Child(_mask, "Body")), fillSprite, color);
                // Calm and rich, not busy: large dark clouds and a few small crack fragments in a darker
                // shade of the liquid, so they sit inside it instead of glowing on top (R-040).
                var shade = Color.Lerp(color, Color.black, 0.55f);
                _mottle = Layer("Clots", theme.Texture("bar_mottle"), new Color(0f, 0f, 0f, 0.32f), 0f);
                _veins = Layer("Veins", theme.Texture("bar_veins"), new Color(shade.r, shade.g, shade.b, 0.3f), 0.3f);
            }
            _bubbles = Layer("Bubbles", theme.Texture("bar_bubbles"), new Color(_light.r, _light.g, _light.b, motion.PatternAlpha), 0f);
            if (motion.CounterSpeed != 0f)
                _counterBubbles = Layer("CounterBubbles", theme.Texture("bar_bubbles"), new Color(_light.r, _light.g, _light.b, motion.PatternAlpha * 0.6f), 0.5f);

            var surfaceRt = Ui.Child(_mask, "Surface");
            surfaceRt.anchorMin = new Vector2(0f, 1f);
            surfaceRt.anchorMax = new Vector2(1f, 1f);
            surfaceRt.pivot = new Vector2(0.5f, 1f);
            surfaceRt.sizeDelta = new Vector2(0f, 2f);
            _surface = Ui.Image(surfaceRt, null, new Color(_light.r, _light.g, _light.b, 0.4f));

            var glintSprite = theme.Sprite("bar_glint");
            if (glintSprite != null)
            {
                float glintWidth = _areaWidth * 0.55f;
                _glintTravel = (_areaWidth - glintWidth) * 0.5f;
                _glintRt = Ui.Place(Ui.Child(_mask, "Glint"), new Vector2(0.5f, 1f), new Vector2(0f, -2.5f), new Vector2(glintWidth, 5f));
                _glintRt.pivot = new Vector2(0.5f, 0.5f);
                _glint = Ui.Image(_glintRt, glintSprite, new Color(_light.r, _light.g, _light.b, 0.5f));
            }

            // Embers live on the bar's root so they can drift past the frame.
            var emberSprite = theme.Sprite("ember");
            for (int i = 0; i < EmberCount; i++)
            {
                var rt = Ui.Place(Ui.Child(Root, "Ember" + i), Vector2.zero, Vector2.zero, new Vector2(5f, 5f));
                rt.pivot = new Vector2(0.5f, 0.5f);
                var img = Ui.Image(rt, emberSprite, motion.Hot);
                img.enabled = false;
                _embers[i] = new Ember { Rt = rt, Image = img };
            }

            // The number: on the value plate when the art has one, else centred on the liquid.
            var plateSprite = theme.Sprite("bar_value");
            if (plateSprite != null)
            {
                float plateWidth = size.x * PlateWidth;
                float plateHeight = plateWidth * plateSprite.rect.height / Mathf.Max(1f, plateSprite.rect.width);
                var plateRt = Ui.Place(Ui.Child(Root, "ValuePlate"), new Vector2(0.5f, 0f),
                    new Vector2(0f, c.y + _areaHeight * PlateAt - plateHeight / 2f), new Vector2(plateWidth, plateHeight));
                Ui.Image(plateRt, plateSprite, Color.white);
                _valueRoot = plateRt.gameObject;
                var inner = theme.Content("bar_value", new Vector4(6f, 6f, 6f, 6f));
                _number = Ui.Fit(Ui.Text(plateRt, "Value", theme, FontRole.Display, 17f, ThemeRuntime.ToUnity(theme.Tokens.TextTitle),
                                         TextAlignmentOptions.Center, outlined: true), 11f);
                Ui.Fill((RectTransform)_number.transform, inner.x, inner.y - 2f, inner.z, inner.w - 2f);
            }
            else
            {
                _number = Ui.Fit(Ui.Text(Root, "Value", theme, FontRole.Display, 19f, ThemeRuntime.ToUnity(theme.Tokens.TextTitle),
                                         TextAlignmentOptions.Center, outlined: true), 12f);
                Ui.Fill((RectTransform)_number.transform, c.x - 1f, c.y, c.z - 1f, c.w);
                _valueRoot = _number.gameObject;
            }

            // The burn as light (D-033) replaces the grain band and the rising burn embers when the shader
            // is available; outside the opening mask so its halo can leak past the frame.
            _burnLight = BurnLight.Create(theme, Root, area, vertical: true);
            if (_burnLight != null && _burn != null) _burn.enabled = false;
        }

        private readonly BurnLight _burnLight;

        /// <summary>Shows or hides the number (and its plate): <c>[Vitals] ShowValues</c>.</summary>
        public void ShowValue(bool show)
        {
            if (_valueRoot.activeSelf != show) _valueRoot.SetActive(show);
        }

        public void SetVisible(bool visible)
        {
            if (Root.gameObject.activeSelf != visible) Root.gameObject.SetActive(visible);
        }

        /// <param name="danger">0..1 pulse strength of the low-value alert (0 = none).</param>
        public void Apply(BarAnimator bar, float danger, float deltaSeconds)
        {
            _time += deltaSeconds;

            if (Mathf.Abs(bar.Fast - _shownFast) > 0.0005f)
            {
                _shownFast = bar.Fast;
                _mask.sizeDelta = new Vector2(0f, _areaHeight * bar.Fast);
                bool any = bar.Fast > 0.001f;
                if (_surface.enabled != any) _surface.enabled = any;
                if (_glint != null && _glint.enabled != any) _glint.enabled = any;
            }

            // The burning span: just above the liquid's surface, as long as something is still
            // draining, but only a short edge (a long trail looked bad after sustained use, R-040).
            float burning = Mathf.Max(0f, bar.Slow - bar.Fast);
            _burnMask.anchoredPosition = new Vector2(0f, _areaHeight * bar.Fast);
            _burnMask.sizeDelta = new Vector2(0f, Mathf.Min(_areaHeight * burning, MaxBurnHeight));
            if (_burnLight != null) _burnLight.Set(bar.Fast, bar.Slow, Mathf.Clamp01(burning * 10f));
            else if (_burn != null)
            {
                var uv = _burn.uvRect;
                uv.x = Mathf.Repeat(uv.x + deltaSeconds * 0.35f, 1f);   // the grains shimmer sideways
                _burn.uvRect = uv;
            }

            if (bar.DisplayChanged) _number.SetText("{0}", bar.Display); // SetText with an int does not allocate
            // Low health: the frame itself pulses red, clearly (R-040: a soft halo was too weak and
            // drew a red box around the bar).
            var frame = danger > 0f ? Color.Lerp(_frameColor, _dangerColor, danger) : _frameColor;
            if (_frame.color != frame) _frame.color = frame;

            // Any change of value flashes the surface briefly.
            if (_lastFast >= 0f && Mathf.Abs(bar.Fast - _lastFast) > 0.0005f) _activity = 1f;
            _lastFast = bar.Fast;
            _activity = Mathf.Max(0f, _activity - deltaSeconds * 1.8f);
            float surfaceAlpha = 0.4f + _activity * 0.45f;
            if (!Mathf.Approximately(_surface.color.a, surfaceAlpha))
                _surface.color = new Color(_light.r, _light.g, _light.b, surfaceAlpha);
            if (_glint != null)
            {
                float glintAlpha = 0.45f + _activity * 0.3f;
                if (!Mathf.Approximately(_glint.color.a, glintAlpha)) _glint.color = new Color(_light.r, _light.g, _light.b, glintAlpha);
                _glintRt.anchoredPosition = new Vector2(_glintTravel * Mathf.Sin(_time * 0.72f), -2.5f);
            }

            if (_liquid != null) _liquid.Scroll(deltaSeconds);
            if (_liquidShimmer != null) _liquidShimmer.Scroll(deltaSeconds);
            Drift(_veins, _motion.Speed, 0.3f, 0.012f, deltaSeconds);
            Drift(_mottle, _motion.Speed * 0.55f, 0f, 0.008f, deltaSeconds);
            Drift(_bubbles, _motion.Speed * 1.6f, 0f, 0.018f, deltaSeconds);
            Drift(_counterBubbles, _motion.CounterSpeed * 1.6f, 0.5f, 0.018f, deltaSeconds);

            // Embers: from the burning surface while something is being consumed, and along the
            // sides while health is low.
            if (_burnLight == null) _emitBurn += deltaSeconds * Mathf.Clamp01(burning * 6f) * 22f;
            _emitSides += deltaSeconds * (danger > 0f ? 5f : 0f);
            while (_emitBurn >= 1f) { _emitBurn -= 1f; Emit(fromSides: false, bar.Fast); }
            while (_emitSides >= 1f) { _emitSides -= 1f; Emit(fromSides: true, bar.Fast); }
            UpdateEmbers(deltaSeconds);
        }

        private void Emit(bool fromSides, float fast)
        {
            for (int i = 0; i < EmberCount; i++)
            {
                if (_embers[i].Life > 0f) continue;
                var e = _embers[i];
                float r = (float)_random.NextDouble();
                if (fromSides)
                {
                    float side = _random.Next(2) == 0 ? -2f : _size.x + 2f;
                    e.Position = new Vector2(side, _areaBottom + r * _areaHeight);
                    e.Velocity = new Vector2((side < 0f ? -1f : 1f) * (3f + 6f * r), 14f + 12f * r);
                }
                else
                {
                    e.Position = new Vector2(_areaLeft + r * _areaWidth, _areaBottom + _areaHeight * fast);
                    e.Velocity = new Vector2(((float)_random.NextDouble() - 0.5f) * 10f, 18f + 22f * r);
                }
                e.MaxLife = e.Life = 0.7f + 0.7f * (float)_random.NextDouble();
                e.Image.enabled = true;
                _embers[i] = e;
                return;
            }
        }

        private void UpdateEmbers(float dt)
        {
            for (int i = 0; i < EmberCount; i++)
            {
                var e = _embers[i];
                if (e.Life <= 0f) continue;
                e.Life -= dt;
                if (e.Life <= 0f)
                {
                    e.Image.enabled = false;
                    _embers[i] = e;
                    continue;
                }
                e.Velocity.y += 6f * dt;
                e.Position += e.Velocity * dt;
                e.Rt.anchoredPosition = e.Position;
                float t = e.Life / e.MaxLife;
                e.Image.color = new Color(_motion.Hot.r, _motion.Hot.g, _motion.Hot.b, t * 0.95f);
                float size = 2.5f + 3.5f * t;
                e.Rt.sizeDelta = new Vector2(size, size);
                _embers[i] = e;
            }
        }

        private void Drift(RawImage layer, float speed, float baseX, float sway, float dt)
        {
            if (layer == null || speed == 0f) return;
            var uv = layer.uvRect;
            uv.y = Mathf.Repeat(uv.y - speed * dt, 1f); // lower v = the texture moves up
            uv.x = baseX + sway * Mathf.Sin(_time * 1.3f + baseX * 6f);
            layer.uvRect = uv;
        }

        private RawImage Layer(string name, Texture texture, Color color, float xOffset)
        {
            if (texture == null) return null;
            var rt = FullHeight(Ui.Child(_mask, name));
            var raw = rt.gameObject.AddComponent<RawImage>();
            raw.texture = texture;
            raw.color = color;
            raw.raycastTarget = false;
            raw.uvRect = new Rect(xOffset, 0f, 1f, _tileHeightUv);
            return raw;
        }

        private RectTransform FullHeight(RectTransform rt)
        {
            Bottom(rt, 0f, _areaHeight);
            return rt;
        }

        private static void Bottom(RectTransform rt, float y, float height)
        {
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, y);
            rt.sizeDelta = new Vector2(0f, height);
        }
    }
}
