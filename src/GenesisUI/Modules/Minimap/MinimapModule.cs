using System.Collections.Generic;
using BepInEx.Configuration;
using GenesisUI.Foundation;
using GenesisUI.Foundation.Contracts;
using GenesisUI.Host;
using GenesisUI.Theme;
using GenesisUI.Widgets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GenesisUI.Modules.Minimap
{
    /// <summary>
    /// Round minimap at the top-right (Diego, R-020): a gold ring, a crest on top with wind and
    /// day/time, the biome name on a banner below.
    ///
    /// It MIRRORS the vanilla small map instead of redrawing it: same texture and the same
    /// material (fog of war included, so nothing unexplored is ever revealed), the same uvRect,
    /// and a copy of every image under the vanilla pin root, so pins from other mods appear
    /// too. Vanilla's Minimap.Start REPLACES the small-map material with an instance and only then
    /// sets the map textures on it; our module can be built before that Start, so the material is
    /// followed every frame (R-030: holding the pre-Start material showed a grey map). The map
    /// itself is a circular mesh, so Unity's stencil Mask cannot make a stale material copy.
    /// Only the ordinary UI pin images sit under a Mask.
    /// </summary>
    [GameContract("assembly_valheim", "Minimap", "instance")]
    [GameContract("assembly_valheim", "Minimap", "m_smallRoot")]
    [GameContract("assembly_valheim", "Minimap", "m_mapImageSmall")]
    [GameContract("assembly_valheim", "Minimap", "m_pinRootSmall")]
    [GameContract("assembly_valheim", "Minimap", "m_smallMarker")]
    [GameContract("assembly_valheim", "Minimap", "m_smallShipMarker")]
    [GameContract("assembly_valheim", "Minimap", "m_windMarker")]
    [GameContract("assembly_valheim", "Minimap", "m_biomeNameSmall")]
    [GameContract("assembly_valheim", "EnvMan", "instance")]
    [GameContract("assembly_valheim", "EnvMan", "GetDay", Parameters = new string[0])]
    [GameContract("assembly_valheim", "EnvMan", "GetDayFraction")]
    [GameContract("assembly_valheim", "EnvMan", "GetWindIntensity")]
    internal sealed class MinimapModule : IUiModule
    {
        private const float RingSize = 250f;
        private const float DefaultMapSize = 222f;    // window radius 111 of 125 (art/src/map_ring.svg)
        private float MapSize = DefaultMapSize;       // the ring's declared window (content insets) when the art has one
        private const int MaxPins = 256;

        private static readonly string[] OwnedRegions = { "hud.minimap" };
        private readonly ConfigEntry<int> _offsetX;
        private readonly ConfigEntry<int> _offsetY;
        private readonly ConfigEntry<float> _scale;

        private ModuleContext _context;
        private RectTransform _group;
        private RectTransform _mapRoot;
        private CircularMapImage _map;
        private RectTransform _pinRoot;
        private readonly List<MirrorImage> _pins = new List<MirrorImage>(64);
        private MirrorImage _marker;
        private MirrorImage _shipMarker;
        private RectTransform _wind;
        private Image _windImage;
        private TextMeshProUGUI _time;
        private TextMeshProUGUI _biome;
        private string _timeFormat;
        private int _shownMinute = -1;
        private int _shownDay = -1;
        private string _shownBiome;
        private Vector2 _appliedOffset = new Vector2(float.NaN, float.NaN);
        private float _appliedScale = float.NaN;

        private sealed class MirrorImage
        {
            public RectTransform Rt;
            public Image Image;
            public RectTransform ChildRt;
            public Image Child;
        }

        public MinimapModule(ConfigFile config)
        {
            _offsetX = config.Bind("Minimap", "OffsetX", 24,
                new ConfigDescription("Distância do minimapa até a borda direita da tela.", new AcceptableValueRange<int>(0, 1800)));
            _offsetY = config.Bind("Minimap", "OffsetY", 1,
                new ConfigDescription("Distância do minimapa até o topo da tela.", new AcceptableValueRange<int>(0, 1000)));
            _scale = config.Bind("Minimap", "Scale", 1f,
                new ConfigDescription("Tamanho do minimapa (1 = padrão).", new AcceptableValueRange<float>(0.5f, 2f)));
        }

        public string Id => "hud.minimap";
        public string NameToken => "$genesisui_module_minimap";
        public IReadOnlyList<string> Regions => OwnedRegions;
        public float RefreshRate => 0f; // every frame: the map follows the player

        public void Build(ModuleContext context)
        {
            _context = context;
            var theme = context.Theme;
            var t = theme.Tokens;
            var mm = global::Minimap.instance;

            // The map window: what the ring's art declares (D-027), else the generated ring's.
            var window = theme.Content("map_ring", new Vector4((RingSize - DefaultMapSize) / 2f, 0f, (RingSize - DefaultMapSize) / 2f, 0f));
            MapSize = RingSize - window.x - window.z;

            // Group pivot at the top-right; the crest sits above the ring.
            _group =Ui.Place(Ui.Child(context.Root, "Minimap"), new Vector2(1f, 1f), Vector2.zero, new Vector2(RingSize, RingSize + 40f));

            _mapRoot = Ui.Place(Ui.Child(_group, "MapWindow"), new Vector2(0.5f, 0f), new Vector2(0f, (RingSize - MapSize) / 2f), new Vector2(MapSize, MapSize));
            var mapRt = Ui.Fill(Ui.Child(_mapRoot, "Map"));
            _map = mapRt.gameObject.AddComponent<CircularMapImage>();
            _map.raycastTarget = false;
            if (mm != null && mm.m_mapImageSmall != null)
            {
                _map.texture = MapTexture(mm.m_mapImageSmall);
                _map.material = mm.m_mapImageSmall.material; // vanilla's instance, fog of war included
            }
            GenesisLog.Info("Module:hud.minimap", "using a direct circular map mesh; stencil masks only pins (" +
                MapTextures(_map.material) + ")");

            var maskImage = Ui.Image(Ui.Fill(Ui.Child(_mapRoot, "PinMask")), theme.Sprite("map_mask"), Color.white);
            var mask = maskImage.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            var content = (RectTransform)maskImage.transform;
            _pinRoot = Ui.Place(Ui.Child(content, "Pins"), Vector2.zero, Vector2.zero, new Vector2(MapSize, MapSize));
            _shipMarker = NewMirror(content, "ShipMarker");
            _marker = NewMirror(content, "PlayerMarker");

            var ring = theme.Sprite("map_ring");
            if (ring != null)
                Ui.Image(Ui.Place(Ui.Child(_group, "Ring"), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(RingSize, RingSize)), ring, Color.white);

            var north = Ui.Text(_group, "North", theme, FontRole.Display, 13f, ThemeRuntime.ToUnity(t.AccentGoldBright), TextAlignmentOptions.Center, outlined: true);
            Ui.Place((RectTransform)north.transform, new Vector2(0.5f, 0f), new Vector2(0f, RingSize - 40f), new Vector2(30f, 18f));
            north.text = "N";

            // Top plate: wind and "Dia 4 · 07:26". The same plate as the biome's below (Diego, R-046),
            // over the ring's top edge as the biome's is over its bottom, drawn after the ring.
            var crestRt = Ui.Place(Ui.Child(_group, "Crest"), new Vector2(0.5f, 0f), new Vector2(0f, RingSize - 18f), new Vector2(180f, 32f));
            Frame.Dress(crestRt, theme, "map_banner", "Minimap", 32f);
            var disk = Ui.Place(Ui.Child(crestRt, "WindDisk"), new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(22f, 22f));
            disk.pivot = new Vector2(0f, 0.5f);
            Ui.Image(disk, theme.Sprite("wind_disk"), Color.white);
            _wind = Ui.Place(Ui.Child(disk, "Wind"), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(18f, 18f));
            _wind.pivot = new Vector2(0.5f, 0.5f);
            _windImage = Ui.Image(_wind, theme.Sprite("wind_arrow"), Color.white);
            _time = Ui.Fit(Ui.Text(crestRt, "DayTime", theme, FontRole.Label, 14f, ThemeRuntime.ToUnity(t.TextTitle), TextAlignmentOptions.Center, outlined: true), 10f);
            Ui.Fill((RectTransform)_time.transform, 50f, 2f, 22f, 2f);
            _timeFormat = (Localization.instance != null ? Localization.instance.Localize("$genesisui_day") : "Day") + " {0} · {1:00}:{2:00}";

            // Banner: biome name, under the ring.
            var bannerRt = Ui.Place(Ui.Child(_group, "Banner"), new Vector2(0.5f, 0f), new Vector2(0f, -14f), new Vector2(180f, 32f));
            Frame.Dress(bannerRt, theme, "map_banner", "Minimap", 32f);
            _biome = Ui.Text(bannerRt, "Biome", theme, FontRole.Label, 14f, ThemeRuntime.ToUnity(t.AccentGoldBright), TextAlignmentOptions.Center, outlined: true);
            Ui.Fill((RectTransform)_biome.transform, 18f, 2f, 18f, 2f);

            _pins.Clear();
            _shownMinute = _shownDay = -1;
            _shownBiome = null;
            _appliedOffset = new Vector2(float.NaN, float.NaN);
            _appliedScale = float.NaN;
        }

        public void Refresh(float deltaSeconds)
        {
            if (_group == null) return;
            var offset = new Vector2(-_offsetX.Value, -_offsetY.Value);
            if (offset != _appliedOffset) { _group.anchoredPosition = offset; _appliedOffset = offset; }
            if (_scale.Value != _appliedScale) { _group.localScale = Vector3.one * _scale.Value; _appliedScale = _scale.Value; }

            var mm = global::Minimap.instance;
            // Vanilla deactivates its small root for the big map and in no-map worlds; follow it.
            bool show = mm != null && mm.m_smallRoot != null && mm.m_smallRoot.activeInHierarchy && Player.m_localPlayer != null;
            if (_group.gameObject.activeSelf != show) _group.gameObject.SetActive(show);
            if (!show) return;

            var source = mm.m_mapImageSmall;
            if (_map.material != source.material)
            {
                // First frames after load, or any later swap: follow vanilla's live instance.
                _map.material = source.material;
                GenesisLog.Info("Module:hud.minimap", "following vanilla's map material '" +
                    (source.material != null ? source.material.name : "null") + "' (" + MapTextures(source.material) + ")");
            }
            var texture = MapTexture(source);
            if (_map.texture != texture) _map.texture = texture;
            _map.uvRect = source.uvRect;

            var reference = source.rectTransform;
            MirrorPins(mm.m_pinRootSmall, reference);
            Mirror(mm.m_smallShipMarker, reference, _shipMarker);
            Mirror(mm.m_smallMarker, reference, _marker);

            if (mm.m_windMarker != null)
                _wind.localEulerAngles = new Vector3(0f, 0f, mm.m_windMarker.eulerAngles.z);
            var env = EnvMan.instance;
            if (env != null)
            {
                // Stronger wind = a fuller, larger arrow; it never fades out of sight.
                float intensity = Mathf.Clamp01(env.GetWindIntensity());
                float a = 0.75f + 0.25f * intensity;
                if (!Mathf.Approximately(_windImage.color.a, a)) _windImage.color = new Color(1f, 1f, 1f, a);
                float s = 0.85f + 0.2f * intensity;
                if (!Mathf.Approximately(_wind.localScale.x, s)) _wind.localScale = new Vector3(s, s, 1f);

                int day = env.GetDay();
                int minute = Mathf.Clamp((int)(env.GetDayFraction() * 1440f), 0, 1439);
                if (minute != _shownMinute || day != _shownDay)
                {
                    _shownMinute = minute;
                    _shownDay = day;
                    _time.SetText(_timeFormat, day, minute / 60, minute % 60);
                }
            }

            var biome = mm.m_biomeNameSmall != null ? mm.m_biomeNameSmall.text : null;
            if (biome != _shownBiome)
            {
                _shownBiome = biome;
                _biome.text = biome ?? "";
            }
        }

        public void Teardown()
        {
            if (_group != null) Object.Destroy(_group.gameObject);
            _group = null;
            _pins.Clear();
        }

        private static Texture MapTexture(RawImage source)
        {
            if (source.texture != null) return source.texture;
            var material = source.material;
            return material != null && material.HasProperty("_MainTex") ? material.GetTexture("_MainTex") : null;
        }

        private static string MapTextures(Material material)
        {
            if (material == null) return "no material";
            return "main=" + TextureName(material, "_MainTex") +
                   ", mask=" + TextureName(material, "_MaskTex") +
                   ", height=" + TextureName(material, "_HeightTex") +
                   ", fog=" + TextureName(material, "_FogTex");
        }

        private static string TextureName(Material material, string property)
        {
            if (!material.HasProperty(property)) return "absent";
            var texture = material.GetTexture(property);
            return texture != null ? texture.name : "null";
        }

        private void MirrorPins(RectTransform source, RectTransform reference)
        {
            int used = 0;
            if (source != null)
            {
                for (int i = 0; i < source.childCount && used < MaxPins; i++)
                {
                    var child = source.GetChild(i) as RectTransform;
                    if (child == null || !child.gameObject.activeInHierarchy) continue;
                    var image = child.GetComponent<Image>();
                    if (image == null || !image.enabled || image.sprite == null) continue;
                    if (used >= _pins.Count) _pins.Add(NewMirror(_pinRoot, "Pin" + used));
                    Copy(child, image, reference, _pins[used]);
                    used++;
                }
            }
            for (int i = used; i < _pins.Count; i++)
                if (_pins[i].Rt.gameObject.activeSelf) _pins[i].Rt.gameObject.SetActive(false);
        }

        private void Mirror(RectTransform source, RectTransform reference, MirrorImage target)
        {
            var image = source != null ? source.GetComponent<Image>() : null;
            bool show = image != null && source.gameObject.activeInHierarchy && image.enabled;
            if (target.Rt.gameObject.activeSelf != show) target.Rt.gameObject.SetActive(show);
            if (show) Copy(source, image, reference, target);
        }

        private void Copy(RectTransform source, Image image, RectTransform reference, MirrorImage target)
        {
            if (!target.Rt.gameObject.activeSelf) target.Rt.gameObject.SetActive(true);
            Rect r = reference.rect;
            float scale = r.width > 0f ? MapSize / r.width : 1f;
            Vector3 local = reference.InverseTransformPoint(source.position);
            var normalized = new Vector2((local.x - r.xMin) / r.width, (local.y - r.yMin) / r.height);
            target.Rt.anchoredPosition = normalized * MapSize;
            target.Rt.sizeDelta = source.rect.size * scale;
            target.Rt.localRotation = source.localRotation;
            if (target.Image.sprite != image.sprite) target.Image.sprite = image.sprite;
            if (target.Image.color != image.color) target.Image.color = image.color;

            // One nested image (a pin's "Checked" cross), mirrored in place.
            Image nested = null;
            for (int i = 0; i < source.childCount; i++)
            {
                var c = source.GetChild(i);
                if (!c.gameObject.activeSelf) continue;
                nested = c.GetComponent<Image>();
                if (nested != null && nested.enabled && nested.sprite != null) break;
                nested = null;
            }
            bool showNested = nested != null;
            if (target.Child.enabled != showNested) target.Child.enabled = showNested;
            if (showNested)
            {
                var nrt = (RectTransform)nested.transform;
                target.ChildRt.sizeDelta = nrt.rect.size * scale;
                target.ChildRt.anchoredPosition = (Vector2)nrt.localPosition * scale;
                if (target.Child.sprite != nested.sprite) target.Child.sprite = nested.sprite;
                if (target.Child.color != nested.color) target.Child.color = nested.color;
            }
        }

        private static MirrorImage NewMirror(RectTransform parent, string name)
        {
            var rt = Ui.Child(parent, name);
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            var image = rt.gameObject.AddComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;
            var childRt = Ui.Child(rt, "Nested");
            childRt.anchorMin = childRt.anchorMax = childRt.pivot = new Vector2(0.5f, 0.5f);
            var child = childRt.gameObject.AddComponent<Image>();
            child.raycastTarget = false;
            child.enabled = false;
            return new MirrorImage { Rt = rt, Image = image, ChildRt = childRt, Child = child };
        }
    }
}
