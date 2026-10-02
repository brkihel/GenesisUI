using System;
using System.Collections.Generic;
using System.IO;
using GenesisUI.Foundation;
using Jotunn.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace GenesisUI.Theme
{
    internal enum FontRole
    {
        /// <summary>Cinzel SemiBold: panel titles, VIDA, VIGOR, creature names.</summary>
        Display,
        /// <summary>Cinzel Medium: tabs, buttons, categories.</summary>
        Label,
        /// <summary>Cormorant Garamond Medium: descriptions, tooltips, lore.</summary>
        Body,
        /// <summary>Cormorant Garamond SemiBold.</summary>
        BodyStrong,
        /// <summary>Cormorant Garamond Medium Italic: flavour lines.</summary>
        BodyItalic,
    }

    /// <summary>
    /// Fonts, sprites and colour tokens as Unity objects (docs/ARCHITECTURE.md §7).
    /// Loaded once, on the first GUI (after Jötunn has the game fonts). Every failure
    /// degrades to the game font or a plain rectangle and is logged; the UI never ends
    /// up without text.
    /// </summary>
    internal sealed class ThemeRuntime
    {
        private static readonly Dictionary<FontRole, string> FontFiles = new Dictionary<FontRole, string>
        {
            [FontRole.Display] = "Cinzel-SemiBold.ttf",
            [FontRole.Label] = "Cinzel-Medium.ttf",
            [FontRole.Body] = "CormorantGaramond-Medium.ttf",
            [FontRole.BodyStrong] = "CormorantGaramond-SemiBold.ttf",
            [FontRole.BodyItalic] = "CormorantGaramond-MediumItalic.ttf",
        };

        private const long MaxFontBytes = 4L * 1024 * 1024;
        private const long MaxImageBytes = 4L * 1024 * 1024;
        private const long MaxManifestBytes = 64L * 1024;

        private readonly string _pluginDir;
        private readonly Dictionary<string, SpriteEntry> _entries = new Dictionary<string, SpriteEntry>(StringComparer.Ordinal);
        private readonly Dictionary<FontRole, TMP_FontAsset> _fonts = new Dictionary<FontRole, TMP_FontAsset>();
        private readonly Dictionary<FontRole, Material> _outlined = new Dictionary<FontRole, Material>();
        private readonly Dictionary<string, Sprite> _sprites = new Dictionary<string, Sprite>(StringComparer.Ordinal);
        private TMP_FontAsset _vanilla;
        private bool _loaded;

        public ThemeTokens Tokens { get; } = ThemeTokens.Default();

        public ThemeRuntime(string pluginDir)
        {
            _pluginDir = pluginDir;
            Current = this;
        }

        /// <summary>The theme in use, for helpers that build images without a theme parameter (Ui.Image).</summary>
        internal static ThemeRuntime Current { get; private set; }

        // ---- shaders (D-033): metal frames and the burn light, from art/genesisui.shaders

        private const long MaxBundleBytes = 8L * 1024 * 1024;
        private readonly Dictionary<Sprite, Sprite> _relief = new Dictionary<Sprite, Sprite>();
        private Material _metal;
        private Shader _burnShader;
        private Shader _blurShader;
        // The light effects and the backdrop (beam, reveal, shine, backdrop), by shader name.
        private readonly Dictionary<string, Shader> _shaders = new Dictionary<string, Shader>();
        private Material _backdrop;

        /// <summary>[Theme] MenuBlur: the pause menu blurs the game behind it when the shader works here.</summary>
        public bool BlurEnabled = true;

        /// <summary>A new material for a blurred backdrop; null when unavailable or turned off.</summary>
        public Material NewBlurMaterial() =>
            BlurEnabled && _blurShader != null ? new Material(_blurShader) { name = "GenesisUI blur", hideFlags = HideFlags.DontSave } : null;

        /// <summary>[Theme] MetalShader: frames lit by the metal shader when it is available.</summary>
        public bool MetalEnabled = true;

        /// <summary>Whether frames are drawn by the metal shader (bundle loaded, supported, enabled).</summary>
        public bool MetalActive => MetalEnabled && _metal != null;

        public bool BurnAvailable => _burnShader != null;

        /// <summary>
        /// Swaps a frame's lit sprite for its relief map and the metal material. Frames without a relief
        /// map, or with the shader unavailable, keep their lit sprite: the same look, without the motion.
        /// </summary>
        public bool Metalize(Image image)
        {
            if (!MetalActive || image == null || image.sprite == null) return false;
            if (!_relief.TryGetValue(image.sprite, out var relief)) return false;
            image.sprite = relief;
            image.material = _metal;
            return true;
        }

        /// <summary>A new material for one bar's burn light; null when the shader is not available.</summary>
        public Material NewBurnMaterial() =>
            _burnShader != null ? new Material(_burnShader) { name = "GenesisUI burn light", hideFlags = HideFlags.DontSave } : null;

        /// <summary>[Theme] LightEffects: the tab beam, the panels' reveal and the craft-ready shine.</summary>
        public bool LightsEnabled = true;

        /// <summary>[Theme] Models3D: the character and the item drawn in 3D in the inventory (D-034).</summary>
        public bool ModelsEnabled = true;

        /// <summary>A new material for one light effect (GenesisUI/Beam, Reveal, Shine); null when off or unavailable.</summary>
        public Material NewLightMaterial(string shader) =>
            LightsEnabled && _shaders.TryGetValue(shader, out var s) ? new Material(s) { name = shader, hideFlags = HideFlags.DontSave } : null;

        /// <summary>A new material of one of the bundle's shaders, not gated by any option; null when unavailable.</summary>
        public Material NewMaterial(string shader) =>
            _shaders.TryGetValue(shader, out var s) ? new Material(s) { name = shader, hideFlags = HideFlags.DontSave } : null;

        /// <summary>The shared material of the windows' backdrop (warm vignette); null when unavailable.</summary>
        public Material BackdropMaterial
        {
            get
            {
                if (_backdrop == null && _shaders.TryGetValue("GenesisUI/Backdrop", out var s))
                    _backdrop = new Material(s) { name = "GenesisUI backdrop", hideFlags = HideFlags.DontSave };
                return _backdrop;
            }
        }

        private void LoadShaders()
        {
            string path = Path.Combine(Path.Combine(_pluginDir, "art"), "genesisui.shaders");
            var info = new FileInfo(path);
            if (!info.Exists) { GenesisLog.Info("Theme", "no shader bundle: frames use their lit sprites"); return; }
            if (info.Length > MaxBundleBytes) { GenesisLog.Warn("Theme", "shader bundle too large; ignored"); return; }
            // An AssetBundle holds assets only (no code); this one holds our two shaders.
            var bundle = AssetBundle.LoadFromFile(path);
            if (bundle == null) { GenesisLog.Warn("Theme", "shader bundle could not be loaded (other platform or game version?)"); return; }
            try
            {
                foreach (var shader in bundle.LoadAllAssets<Shader>())
                {
                    if (shader == null) continue;
                    if (!shader.isSupported) { GenesisLog.Warn("Theme", "shader not supported on this GPU/API: " + shader.name); continue; }
                    if (shader.name == "GenesisUI/Metal") _metal = new Material(shader) { name = "GenesisUI metal", hideFlags = HideFlags.DontSave };
                    else if (shader.name == "GenesisUI/Burn") _burnShader = shader;
                    else if (shader.name == "GenesisUI/Blur") _blurShader = shader;
                    else if (shader.name.StartsWith("GenesisUI/", System.StringComparison.Ordinal)) _shaders[shader.name] = shader;
                }
            }
            finally
            {
                bundle.Unload(false); // keeps the loaded shaders
            }
            GenesisLog.Info("Theme", "shaders: metal " + (_metal != null ? "on" : "off") + ", burn " + (_burnShader != null ? "on" : "off") +
                ", blur " + (_blurShader != null ? "on" : "off") + ", effects [" + string.Join(", ", _shaders.Keys) + "]" +
                " (" + SystemInfo.graphicsDeviceType + ")");
        }

        /// <summary>
        /// Where a frame's contents go, in design units (left, bottom, right, top), as declared in
        /// art/sprites.json; <paramref name="fallback"/> when the sprite does not declare it.
        /// </summary>
        public Vector4 Content(string name, Vector4 fallback) =>
            _entries.TryGetValue(name, out var e) && e.HasContent
                ? new Vector4(e.contentLeft, e.contentBottom, e.contentRight, e.contentTop)
                : fallback;

        /// <summary>A sprite's design size (art/sprites.json); zero when missing.</summary>
        public Vector2 Size(string name) =>
            _entries.TryGetValue(name, out var e) ? new Vector2(e.width, e.height) : Vector2.zero;

        /// <summary>A sprite's 9-slice border in design units (left, bottom, right, top); zero when missing.</summary>
        public Vector4 Border(string name) =>
            _entries.TryGetValue(name, out var e) ? new Vector4(e.borderLeft, e.borderBottom, e.borderRight, e.borderTop) : Vector4.zero;

        /// <summary>An edge ornament's distance from the frame's edge to its centre; 0 when not declared.</summary>
        public float Inset(string name) => _entries.TryGetValue(name, out var e) && e.inset >= 0 ? e.inset : 0f;

        /// <summary>Space between repeated cells of a frame; <paramref name="fallback"/> when not declared.</summary>
        public float Gap(string name, float fallback) => _entries.TryGetValue(name, out var e) && e.gap >= 0 ? e.gap : fallback;

        // ---- panel backgrounds: own opacity per panel (AGENTS.md §2b)

        private readonly List<KeyValuePair<Image, string>> _backgrounds = new List<KeyValuePair<Image, string>>();

        /// <summary>Opacity of a panel's background, by panel key; set by the plugin from [Backgrounds].</summary>
        public Func<string, float> BackgroundOpacity = _ => 0.6f;

        public int BackgroundCount => _backgrounds.Count;

        /// <summary>Brightness of the panel material (1 = as painted).</summary>
        public const float BackgroundShade = 0.55f;

        public void RegisterBackground(Image image, string panel)
        {
            Apply(image, panel);
            _backgrounds.Add(new KeyValuePair<Image, string>(image, panel));
        }

        /// <summary>Re-reads every live background's opacity (a [Backgrounds] value changed); forgets destroyed ones.</summary>
        public void RefreshBackgrounds()
        {
            _backgrounds.RemoveAll(kv => kv.Key == null);
            foreach (var kv in _backgrounds) Apply(kv.Key, kv.Value);
        }

        private void Apply(Image image, string panel)
        {
            float a = Mathf.Clamp01(BackgroundOpacity(panel));
            // Diego's stone, darker (D-033: "a UI toda mais escura").
            image.color = new Color(BackgroundShade, BackgroundShade, BackgroundShade, a);
            // Fully transparent: skip drawing it (and its stencil mask) altogether.
            var clip = image.transform.parent;
            if (clip != null && clip.gameObject.activeSelf != a > 0f) clip.gameObject.SetActive(a > 0f);
        }

        public static Color ToUnity(ColorRgba c) => new Color(c.R, c.G, c.B, c.A);

        public void EnsureLoaded()
        {
            if (_loaded) return;
            _vanilla = GUIManager.Instance.TMP_AveriaSansLibre;
            if (_vanilla == null) return; // Jötunn has not resolved the game fonts yet; try on the next GUI
            _loaded = true;

            Guard.Try("load fonts", LoadFonts);
            Guard.Try("load sprites", LoadSprites);
            Guard.Try("load shaders", LoadShaders);
        }

        public TMP_FontAsset Font(FontRole role) => _fonts.TryGetValue(role, out var f) ? f : _vanilla;

        /// <summary>Shared material with an outline and a soft shadow, for text over the world.</summary>
        public Material OutlinedMaterial(FontRole role)
        {
            if (_outlined.TryGetValue(role, out var m)) return m;
            var font = Font(role);
            if (font == null || font.material == null) return null;

            m = new Material(font.material) { name = font.name + " (GenesisUI outlined)" };
            m.EnableKeyword(ShaderUtilities.Keyword_Outline);
            m.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.18f);
            m.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0f, 0f, 0f, 0.85f));
            m.EnableKeyword(ShaderUtilities.Keyword_Underlay);
            m.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0f, 0f, 0f, 0.6f));
            m.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0.35f);
            m.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.35f);
            m.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.4f);
            _outlined[role] = m;
            return m;
        }

        /// <summary>Null when missing: callers fall back to a plain coloured Image.</summary>
        public Sprite Sprite(string name) => _sprites.TryGetValue(name, out var s) ? s : null;

        /// <summary>The texture behind a sprite, for RawImage patterns that scroll. Null when missing.</summary>
        public Texture2D Texture(string name) => _sprites.TryGetValue(name, out var s) ? s.texture : null;

        public int FontCount => _fonts.Count;
        public int SpriteCount => _sprites.Count;

        private const string RunicFile = "NotoSansRunic-Regular.ttf";

        /// <summary>Whether the runes can be drawn (the runic font loaded).</summary>
        public bool RunesAvailable { get; private set; }

        private TMP_FontAsset LoadFallbackFont(string path)
        {
            var info = new FileInfo(path);
            if (!info.Exists || info.Length > MaxFontBytes) { GenesisLog.Info("Theme", "no runic font: titles appear without runes"); return null; }
            var asset = TMP_FontAsset.CreateFontAsset(path, 0, 90, 9, GlyphRenderMode.SDFAA, 512, 512);
            if (asset == null) return null;
            asset.name = "GenesisUI " + Path.GetFileNameWithoutExtension(path);
            EnsureMaterial(asset);
            RunesAvailable = true;
            return asset;
        }

        private void LoadFonts()
        {
            string dir = Path.Combine(_pluginDir, "fonts");
            foreach (var kv in FontFiles)
            {
                string path = Path.Combine(dir, kv.Value);
                try
                {
                    var info = new FileInfo(path);
                    if (!info.Exists) { GenesisLog.Warn("Theme", "font missing, using the game font: " + kv.Value); continue; }
                    if (info.Length > MaxFontBytes) { GenesisLog.Warn("Theme", "font too large, ignored: " + kv.Value); continue; }

                    var asset = TMP_FontAsset.CreateFontAsset(path, 0, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024);
                    if (asset == null) { GenesisLog.Warn("Theme", "TextMeshPro could not load " + kv.Value + "; using the game font"); continue; }
                    asset.name = "GenesisUI " + Path.GetFileNameWithoutExtension(kv.Value);
                    EnsureMaterial(asset);
                    _fonts[kv.Key] = asset;
                }
                catch (Exception e)
                {
                    GenesisLog.Warn("Theme", "font " + kv.Value + " failed (" + e.GetType().Name + ": " + e.Message + "); using the game font");
                }
            }

            // The runes of the window titles (D-036): Elder Futhark from Noto Sans Runic, a fallback only.
            TMP_FontAsset runic = null;
            Guard.Try("load runic font", () => runic = LoadFallbackFont(Path.Combine(dir, RunicFile)));

            // Fallback chain: Cinzel lacks some symbols (e.g. ◆), Cormorant has them, the runes, then the game font.
            TMP_FontAsset body = _fonts.TryGetValue(FontRole.Body, out var b) ? b : null;
            foreach (var kv in _fonts)
            {
                var chain = new List<TMP_FontAsset>();
                if (body != null && kv.Value != body) chain.Add(body);
                if (runic != null) chain.Add(runic);
                chain.Add(_vanilla);
                kv.Value.fallbackFontAssetTable = chain;
            }
            GenesisLog.Info("Theme", "fonts loaded: " + _fonts.Count + "/" + FontFiles.Count);
        }

        /// <summary>
        /// A runtime font asset gets TMP's default SDF shader through Shader.Find. If the game
        /// build does not include it, borrow the game font's material (same SDF shader family)
        /// and point it at our atlas.
        /// </summary>
        private void EnsureMaterial(TMP_FontAsset asset)
        {
            var mat = asset.material;
            if (mat != null && mat.shader != null && mat.shader.isSupported) return;

            var borrowed = new Material(_vanilla.material) { name = asset.name + " (borrowed shader)" };
            borrowed.SetTexture(ShaderUtilities.ID_MainTex, asset.atlasTexture);
            borrowed.SetFloat(ShaderUtilities.ID_TextureWidth, asset.atlasWidth);
            borrowed.SetFloat(ShaderUtilities.ID_TextureHeight, asset.atlasHeight);
            borrowed.SetFloat(ShaderUtilities.ID_GradientScale, asset.atlasPadding + 1);
            asset.material = borrowed;
            GenesisLog.Info("Theme", asset.name + ": default TMP shader unavailable, using the game font's shader");
        }

        private void LoadSprites()
        {
            string dir = Path.GetFullPath(Path.Combine(_pluginDir, "art"));
            string manifestPath = Path.Combine(dir, "sprites.json");
            var manifestInfo = new FileInfo(manifestPath);
            if (!manifestInfo.Exists) { GenesisLog.Warn("Theme", "art/sprites.json missing; plain shapes will be used"); return; }
            if (manifestInfo.Length > MaxManifestBytes) { GenesisLog.Warn("Theme", "art/sprites.json too large; ignored"); return; }

            // Our own strict reader (D-018): JsonUtility left the sprite list empty without
            // an error in 0.2.0-preview.1, and a silent half-read is the worst outcome.
            var errors = new List<string>();
            var manifest = SpriteManifestValidator.Parse(File.ReadAllText(manifestPath), errors);
            if (errors.Count == 0) errors.AddRange(SpriteManifestValidator.Validate(manifest));
            if (errors.Count > 0)
            {
                GenesisLog.Warn("Theme", "art/sprites.json rejected: " + string.Join("; ", errors));
                return;
            }

            foreach (var entry in manifest.sprites)
            {
                string path = Path.GetFullPath(Path.Combine(dir, entry.file));
                if (!path.StartsWith(dir + Path.DirectorySeparatorChar, StringComparison.Ordinal)) continue; // validated already; belt and braces
                var info = new FileInfo(path);
                if (!info.Exists || info.Length > MaxImageBytes) { GenesisLog.Warn("Theme", "sprite file missing or too large: " + entry.file); continue; }

                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false, entry.color == "linear")
                {
                    name = "GenesisUI " + entry.name,
                    filterMode = FilterMode.Bilinear,
                    wrapMode = entry.wrap == "repeat" ? TextureWrapMode.Repeat : TextureWrapMode.Clamp,
                };
                // Jötunn's wrapper: Unity 6's ImageConversion needs netstandard 2.1, which net48 cannot reference.
                if (!Jotunn.Utils.AssetUtils.LoadImage(tex, File.ReadAllBytes(path)))
                {
                    GenesisLog.Warn("Theme", "not a valid PNG: " + entry.file);
                    UnityEngine.Object.Destroy(tex);
                    continue;
                }
                if (tex.width != entry.width * manifest.scale || tex.height != entry.height * manifest.scale)
                    GenesisLog.Warn("Theme", entry.file + " is " + tex.width + "x" + tex.height + ", manifest expects " + entry.width * manifest.scale + "x" + entry.height * manifest.scale);

                float s = manifest.scale;
                var sprite = UnityEngine.Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f * s, 0,
                    SpriteMeshType.FullRect, new Vector4(entry.borderLeft * s, entry.borderBottom * s, entry.borderRight * s, entry.borderTop * s));
                sprite.name = "GenesisUI " + entry.name;
                _sprites[entry.name] = sprite;
                _entries[entry.name] = entry;
            }
            foreach (var kv in _sprites)
                if (kv.Key.EndsWith("_relief", StringComparison.Ordinal) &&
                    _sprites.TryGetValue(kv.Key.Substring(0, kv.Key.Length - "_relief".Length), out var lit))
                    _relief[lit] = kv.Value;
            GenesisLog.Info("Theme", "sprites loaded " + _sprites.Count + "/" + manifest.sprites.Length + ", " + _relief.Count + " with relief maps");
        }
    }
}
