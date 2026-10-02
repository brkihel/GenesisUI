using System.Collections.Generic;
using GenesisUI.Foundation;
using UnityEngine;
using UnityEngine.UI;

namespace GenesisUI.Widgets
{
    /// <summary>
    /// A small 3D stage drawn into the UI (D-034): its own camera renders only its own layer into a
    /// render texture shown by a RawImage. The stage sits beyond the world, with GenesisUI's gold
    /// key light from above (the tab beam's light), a cool rim from behind and a soft fill. Its lights
    /// (and any light on a model, a torch's flame) are switched on only while the stage's camera
    /// renders and off right after: a light's culling mask is not enough (the world renders deferred,
    /// and Unity takes the brightest directional light as the sun), and left on they lit the whole
    /// world (Diego, 2026-10-02: "ao abrir o inventário fica muito claro").
    /// Stability (Diego: the previews stopped showing after a while, no error anywhere): the camera is
    /// never left enabled for Unity to schedule; the stage renders itself, on demand, once per frame
    /// while shown (<see cref="Camera.Render"/>), with its lights and the fog switched around that one
    /// call. A watchdog checks the texture, the camera and the scene every frame it is shown, repairs
    /// what it finds and logs it once (<see cref="Problem"/>), so a report names any cause. The camera only renders while the RawImage is
    /// shown. What stands on it is a visual copy only: nothing networked, no physics, no game logic
    /// (<see cref="Strip"/>).
    /// </summary>
    [GenesisUI.Foundation.Contracts.ContractDependency(typeof(GenesisUI.Theme.ThemeRuntime), typeof(GenesisUI.Foundation.GenesisLog), typeof(GenesisUI.Widgets.Ui), typeof(GenesisUI.Foundation.Guard))]
    internal sealed class PreviewStage : MonoBehaviour
    {
        // Far outside the world (its radius is 10 500 m) but at an ordinary height, just above the sea
        // (30 m): Valheim's own shaders change what lies under the water level (the stage once sat 10 km
        // down: the hammer and the character drew nothing, a plain-shader item did). No world camera
        // sees the stage's layer, wherever it is.
        private static readonly Vector3 Origin = new Vector3(15000f, 40f, 15000f); // beyond the edge, close enough for float precision
        private const int MaxTexture = 1024;
        private static int _nextSlot;
        private static int _layer = -2;
        private static readonly List<PreviewStage> Stages = new List<PreviewStage>();
        private readonly GenesisUI.Foundation.Logging.RecentSamples _renderSamples = new GenesisUI.Foundation.Logging.RecentSamples();
        private long _renderCount;
        internal static IEnumerable<string> Diagnostics()
        {
            foreach (var stage in Stages)
                if (stage != null) yield return stage._name + ": generation " + stage.Generation + ", shown " + stage._shown + ", renders " + stage._renderCount + ", texture " + stage._size.x + "x" + stage._size.y + ", estimated color+depth bytes " + (long)stage._size.x * stage._size.y * 8 + ", Camera.Render CPU/submission p50/p95 ms " + stage._renderSamples.Percentile(0.5).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "/" + stage._renderSamples.Percentile(0.95).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + ", samples " + stage._renderSamples.Count + "; GPU time not measured";
        }

        private Camera _camera;
        private RawImage _image;
        private RenderTexture _texture;
        private Transform _root, _pivot;
        private readonly List<Light> _lights = new List<Light>(3);
        private Vector2Int _size;
        private bool _shown, _deferred;
        private string _name;
        private Material _keyed;
        private string _owner;
        private System.Action _tick;
        private bool _failed;
        public bool Available => !_failed && _keyed != null;
        internal bool PrepareModel()
        {
            if (!Available) return false;
            if (_root == null || _camera == null || _pivot == null) Healthy();
            return Available && _pivot != null;
        }
        private static readonly Color Key = new Color(1f, 0f, 1f, 1f);
        private int _emptyCheck;
        private readonly HashSet<string> _logged = new HashSet<string>();

        /// <summary>Bumped when the scene had to be rebuilt: owners then put their model back.</summary>
        public int Generation { get; private set; }

        /// <summary>Turning: degrees per second (Spin), or a slow sway of ± Sway degrees around BaseYaw.</summary>
        public float Spin, Sway, BaseYaw;
        private float _yaw;

        /// <summary>Where models go: centred on the stage's origin; turning this turns the model.</summary>
        public Transform Pivot => _pivot;

        /// <summary>The stage's layer, applied to everything under <see cref="Pivot"/> by <see cref="Strip"/>.</summary>
        public static int Layer => _layer;

        /// <summary>
        /// A stage shown in <paramref name="area"/>. Null when no free layer exists (the caller keeps its
        /// 2D look), or when the effects are off.
        /// </summary>
        public static PreviewStage Create(RectTransform area, string name)
        {
            var theme = Theme.ThemeRuntime.Current;
            if (theme != null && !theme.ModelsEnabled) return null;
            if (theme == null || !theme.HasShader("GenesisUI/Keyed"))
            { GenesisLog.Warn("Preview", "keyed compositor unavailable; keeping 2D presentation"); return null; }
            if (_layer == -2) _layer = FindLayer();
            if (_layer < 0) return null;
            var rt = Ui.Fill(Ui.Child(area, "Preview " + name));
            var image = rt.gameObject.AddComponent<RawImage>();
            image.raycastTarget = false;
            image.color = Color.white;
            image.enabled = false;
            var stage = rt.gameObject.AddComponent<PreviewStage>();
            stage._image = image;
            stage._name = name;
            stage._owner = Guard.CurrentOwner ?? "preview:" + name;
            stage._tick = stage.Tick;
            Stages.Add(stage);
            stage.BuildScene(name);
            return stage;
        }

        /// <summary>A layer no camera or light of the game uses: unnamed, and outside the main camera's mask.</summary>
        private static int FindLayer()
        {
            var main = Camera.main;
            int mask = main != null ? main.cullingMask : ~0;
            for (int i = 31; i >= 8; i--)
                if (string.IsNullOrEmpty(LayerMask.LayerToName(i)) && (mask & (1 << i)) == 0) return i;
            // Every free layer is in the main camera's mask: the stage is still beyond the world's edge.
            for (int i = 31; i >= 8; i--)
                if (string.IsNullOrEmpty(LayerMask.LayerToName(i))) return i;
            GenesisLog.Warn("Preview", "no free layer for the 3D previews; they stay 2D");
            return -1;
        }

        private void BuildScene(string name)
        {
            int slot = _nextSlot++;
            _root = OwnerResources.Own(new GameObject("GenesisUI.PreviewStage " + name)).transform;
            _root.position = Origin + new Vector3(slot * 60f, 0f, 0f);
            Object.DontDestroyOnLoad(_root.gameObject);
            _pivot = new GameObject("Pivot").transform;
            _pivot.SetParent(_root, false);

            var cam = new GameObject("Camera").AddComponent<Camera>();
            cam.transform.SetParent(_root, false);
            cam.clearFlags = CameraClearFlags.SolidColor;
            // Valheim's creature/player forward passes can draw RGB without useful target alpha.
            // Compose from a colour key so the UI does not discard those pixels (D-039).
            if (_keyed == null)
            {
                var theme = Theme.ThemeRuntime.Current;
                _keyed = theme != null ? theme.NewMaterial("GenesisUI/Keyed") : null;
                if (_keyed == null) throw new System.InvalidOperationException("Keyed compositor unavailable");
            }
            cam.backgroundColor = _keyed != null ? Key : new Color(0f, 0f, 0f, 0f);
            _image.material = _keyed;
            cam.cullingMask = 1 << _layer;
            cam.fieldOfView = 24f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 30f;
            cam.allowHDR = false;
            cam.allowMSAA = _keyed == null; // key removal must happen before any edge filtering
            cam.useOcclusionCulling = false;
            cam.renderingPath = RenderingPath.Forward; // culling masks and a transparent background work here
            cam.enabled = false; // rendered on demand only (Render)
            _camera = cam;

            // Gold from above (the beam), a cool rim from behind, a soft warm fill from the front.
            AddLight("Key", new Vector3(62f, -18f, 0f), new Color(1f, 0.86f, 0.62f), 1.25f);
            AddLight("Rim", new Vector3(20f, 160f, 0f), new Color(0.62f, 0.72f, 0.95f), 0.7f);
            AddLight("Fill", new Vector3(8f, 25f, 0f), new Color(0.95f, 0.85f, 0.72f), 0.35f);
        }

        private void AddLight(string name, Vector3 euler, Color color, float intensity)
        {
            var light = new GameObject(name).AddComponent<Light>();
            light.transform.SetParent(_root, false);
            light.transform.localRotation = Quaternion.Euler(euler);
            light.type = LightType.Directional;
            light.color = color;
            light.intensity = intensity;
            light.shadows = LightShadows.None;
            light.cullingMask = 1 << _layer; // the stage only: the world never sees these lights
            light.renderMode = LightRenderMode.ForcePixel;
            light.enabled = false; // only during the stage's own render (Render)
            _lights.Add(light);
        }

        /// <summary>Places the camera to frame <paramref name="bounds"/> (world space), looking slightly down.</summary>
        public void Frame(Bounds bounds, float pitch = 8f, float margin = 1.15f)
        {
            float radius = Mathf.Max(0.05f, bounds.extents.magnitude) * margin;
            float aspect = _camera.aspect > 0f ? _camera.aspect : 1f;
            float fov = Mathf.Min(_camera.fieldOfView, Camera.VerticalToHorizontalFieldOfView(_camera.fieldOfView, aspect));
            float distance = radius / Mathf.Sin(fov * 0.5f * Mathf.Deg2Rad);
            var rotation = Quaternion.Euler(pitch, 180f, 0f); // looking at the model's front (+Z faces the camera)
            _camera.transform.position = bounds.center - rotation * Vector3.forward * distance;
            _camera.transform.rotation = rotation;
            _camera.farClipPlane = distance + radius * 2f + 1f;
            _camera.nearClipPlane = Mathf.Max(0.02f, distance - radius * 2f);
        }

        /// <summary>The bounds of every renderer under the pivot (world space); false when there is none.</summary>
        public bool Bounds(out Bounds bounds)
        {
            bounds = default;
            bool any = false;
            _pivot.GetComponentsInChildren(false, _renderers);
            foreach (var r in _renderers)
            {
                if (IsType(r, "UnityEngine.ParticleSystemRenderer")) continue;
                if (!any) { bounds = r.bounds; any = true; }
                else bounds.Encapsulate(r.bounds);
            }
            return any;
        }

        /// <summary>Shows or hides the picture; the stage renders only while shown.</summary>
        public void Show(bool show)
        {
            show = show && !_failed && _keyed != null;
            _shown = show;
            if (_image != null && _image.enabled != show) _image.enabled = show;
        }

        private void LateUpdate()
        {
            if (!_shown || _image == null) return;
            if (!Guard.Run(_owner, _tick)) { _failed = true; Show(false); }
        }

        private void Tick()
        {
            if (!Healthy()) return;
            if (Spin != 0f) { _yaw = Mathf.Repeat(_yaw + Spin * Time.unscaledDeltaTime, 360f); _pivot.localRotation = Quaternion.Euler(0f, _yaw, 0f); }
            else if (Sway != 0f)
            {
                float t = Time.unscaledTime;
                _pivot.localRotation = Quaternion.Euler(0f, BaseYaw + (Mathf.Sin(t * 0.35f) * 0.75f + Mathf.Sin(t * 0.13f + 1.1f) * 0.25f) * Sway, 0f);
            }
            EnsureTexture();
            Render();
            if (_inspect != null && --_inspectIn <= 0) InspectNow();
        }

        /// <summary>
        /// The watchdog: the stage's scene, camera and texture are where they should be; anything else is
        /// repaired (the scene rebuilt, the texture recreated) and logged once with what was found.
        /// </summary>
        private bool Healthy()
        {
            if (_root == null || _camera == null || _pivot == null)
            {
                Problem("scene gone (root " + (_root != null) + ", camera " + (_camera != null) + ", pivot " + (_pivot != null) + "): rebuilt");
                if (_root != null) Object.Destroy(_root.gameObject);
                _lights.Clear();
                BuildScene(_name);
                _size = default;
                Generation++;
                return false; // owners put their model back first
            }
            if (!_root.gameObject.activeSelf) { Problem("scene root was deactivated: reactivated"); _root.gameObject.SetActive(true); }
            if (!_camera.gameObject.activeSelf) { Problem("camera object was deactivated: reactivated"); _camera.gameObject.SetActive(true); }
            if (_camera.enabled) { Problem("camera was enabled by someone else: disabled (the stage renders itself)"); _camera.enabled = false; }
            if (_camera.cullingMask != 1 << _layer) { Problem("camera culling mask changed: restored"); _camera.cullingMask = 1 << _layer; }
            if (_texture != null && !_texture.IsCreated()) { Problem("render texture was released (device reset?): recreated"); _size = default; }
            if (_texture != null && (_camera.targetTexture != _texture || _image.texture != _texture)) { Problem("render texture unbound: rebound"); _size = default; }
            // Now and then: is anything left to draw? (an empty stage shows nothing and says nothing)
            if (++_emptyCheck >= 120)
            {
                _emptyCheck = 0;
                int drawn = 0;
                _pivot.GetComponentsInChildren(false, _renderers);
            foreach (var r in _renderers)
                    if (r.enabled && r.gameObject.layer == _layer) drawn++;
                if (drawn == 0 && _pivot.childCount > 0) Problem("nothing drawable on the stage (" + _pivot.childCount + " object(s), no enabled renderer on layer " + _layer + ")");
            }
            return true;
        }

        private void EnsureTexture()
        {
            // The texture follows the picture's size on screen (sharp at any resolution, capped).
            var rect = ((RectTransform)transform).rect;
            var canvas = _image.canvas;
            float scale = canvas != null ? canvas.scaleFactor : 1f;
            float width = Mathf.Max(16f, rect.width * scale), height = Mathf.Max(16f, rect.height * scale);
            float sampling = Mathf.Min(_keyed != null ? 2f : 1f, MaxTexture / Mathf.Max(width, height));
            var size = new Vector2Int(Mathf.Clamp(Mathf.RoundToInt(width * sampling), 16, MaxTexture),
                                      Mathf.Clamp(Mathf.RoundToInt(height * sampling), 16, MaxTexture));
            if (size == _size && _texture != null) return;
            _size = size;
            if (_texture != null) { _camera.targetTexture = null; _texture.Release(); Object.Destroy(_texture); }
            // Preserve unmixed key texels. The keyed UI shader smooths the decoded samples;
            // a bounded 2x resolution supplies coverage without resolving magenta into the model.
            _texture = new RenderTexture(size.x, size.y, 24, RenderTextureFormat.ARGB32)
            {
                name = "GenesisUI preview", antiAliasing = _deferred || _keyed != null ? 1 : 4,
                filterMode = _keyed != null ? FilterMode.Point : FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            _texture.Create();
            _camera.targetTexture = _texture;
            _camera.aspect = (float)size.x / size.y;
            _image.texture = _texture;
        }

        /// <summary>
        /// One render of the stage: its lights (and any light on a model) on, the world's fog off, render,
        /// then both back — always, even if the render throws. The world's cameras never see these lights.
        /// </summary>
        private void Render()
        {
            long started = System.Diagnostics.Stopwatch.GetTimestamp();
            _root.GetComponentsInChildren(true, _renderLights);
            bool fog = RenderSettings.fog;
            try
            {
                for (int i = 0; i < _renderLights.Count; i++) _renderLights[i].enabled = true;
                RenderSettings.fog = false;
                _camera.Render();
                _renderCount++;
            }
            finally
            {
                for (int i = 0; i < _renderLights.Count; i++) if (_renderLights[i] != null) _renderLights[i].enabled = false;
                RenderSettings.fog = fog;
                _renderSamples.Add((System.Diagnostics.Stopwatch.GetTimestamp() - started) * 1000.0 / System.Diagnostics.Stopwatch.Frequency);
            }
        }

        private readonly List<Light> _renderLights = new List<Light>(8);
        private readonly List<Renderer> _renderers = new List<Renderer>(64);

        // One look at what the stage really draws, a moment after a model is put on it (logged once per model).
        private GameObject _inspect;
        private string _inspectLabel;
        private int _inspectIn;

        /// <summary>Logs, once, what the stage draws of <paramref name="model"/> after a few renders (D-039).</summary>
        public void Inspect(GameObject model, string label)
        {
            _inspect = model;
            _inspectLabel = label;
            _inspectIn = 20;
        }

        private void InspectNow()
        {
            var model = _inspect;
            _inspect = null;
            if (model == null) return;
            int total = 0, enabled = 0, seen = 0;
            var shaders = new Dictionary<string, string>();
            Bounds bounds = default;
            bool any = false;
            foreach (var r in model.GetComponentsInChildren<Renderer>(true))
            {
                total++;
                if (!r.enabled || !r.gameObject.activeInHierarchy) continue;
                enabled++;
                if (r.isVisible) seen++;
                if (!any) { bounds = r.bounds; any = true; } else bounds.Encapsulate(r.bounds);
                foreach (var m in r.sharedMaterials)
                {
                    if (m == null || m.shader == null || shaders.ContainsKey(m.shader.name)) continue;
                    var passes = new List<string>();
                    for (int i = 0; i < m.passCount; i++) passes.Add(m.GetPassName(i));
                    shaders[m.shader.name] = (m.shader.isSupported ? "" : "UNSUPPORTED ") + "q" + m.renderQueue + " [" + string.Join("/", passes) + "]";
                }
            }
            var cam = _camera.transform;
            var sb = new System.Text.StringBuilder();
            sb.Append(_name).Append(" stage, ").Append(_inspectLabel).Append(": renderers ").Append(total)
              .Append(", enabled ").Append(enabled).Append(", visible ").Append(seen)
              .Append("; bounds ").Append(any ? bounds.center.ToString("F2") + " size " + bounds.size.ToString("F2") : "none")
              .Append("; camera ").Append(cam.position.ToString("F2")).Append(" looking ").Append(cam.forward.ToString("F2"))
              .Append(" near ").Append(_camera.nearClipPlane.ToString("F2")).Append(" far ").Append(_camera.farClipPlane.ToString("F2"))
              .Append(", ").Append(_camera.actualRenderingPath)
              .Append("; layer ").Append(_layer).Append(" model layer ").Append(model.layer)
              .Append("; shaders: ");
            foreach (var kv in shaders) sb.Append(kv.Key).Append(' ').Append(kv.Value).Append("; ");
            AppendPixelProbe(sb);
            GenesisLog.Info("Preview", sb.ToString());
        }

        // One small GPU readback per inspected model, never in the steady per-frame path. A renderer
        // being "visible" only proves camera culling accepted it; it does not prove useful pixels.
        private void AppendPixelProbe(System.Text.StringBuilder sb)
        {
            if (_texture == null) return;
            RenderTexture previous = RenderTexture.active;
            RenderTexture small = null;
            Texture2D pixels = null;
            try
            {
                const int side = 48;
                small = RenderTexture.GetTemporary(side, side, 0, RenderTextureFormat.ARGB32);
                Graphics.Blit(_texture, small);
                RenderTexture.active = small;
                pixels = new Texture2D(side, side, TextureFormat.RGBA32, false);
                pixels.ReadPixels(new Rect(0, 0, side, side), 0, 0);
                pixels.Apply(false, false);
                int coloured = 0, colouredZeroAlpha = 0;
                Color32 clear = _keyed != null ? (Color32)Key : new Color32(0, 0, 0, 0);
                foreach (var p in pixels.GetPixels32())
                {
                    // Approximately the keyed shader's tolerance from the actual clear colour.
                    int dr = p.r - clear.r, dg = p.g - clear.g, db = p.b - clear.b;
                    if (dr * dr + dg * dg + db * db <= 1024) continue;
                    coloured++;
                    if (p.a < 16) colouredZeroAlpha++;
                }
                sb.Append(" pixels RGB ").Append(coloured).Append('/').Append(side * side)
                  .Append(", RGB with alpha~0 ").Append(colouredZeroAlpha)
                  .Append(", keyed ").Append(_keyed != null)
                  .Append(", target ").Append(_texture.width).Append('x').Append(_texture.height)
                  .Append(", AA ").Append(_texture.antiAliasing).Append(", filter ").Append(_texture.filterMode);
            }
            catch (System.Exception e) { Problem("pixel inspection failed: " + e.GetType().Name + ": " + e.Message); }
            finally
            {
                RenderTexture.active = previous;
                if (pixels != null) Object.Destroy(pixels);
                if (small != null) RenderTexture.ReleaseTemporary(small);
            }
        }

        /// <summary>
        /// The rendering path the model on the stage needs (D-039). A material with a deferred pass
        /// but no forward pass needs the deferred path. Both paths use GenesisUI/Keyed so output
        /// alpha cannot discard model colour in the UI. Without that shader it stays forward
        /// with a transparent clear colour and logs the limitation.
        /// </summary>
        public void UsePathFor(GameObject model)
        {
            bool deferred = NeedsDeferred(model, out string shaders);
            if (deferred && _keyed == null) { Problem("model needs the deferred path but the keyed shader is missing: it may not show (" + shaders + ")"); deferred = false; }
            if (deferred == _deferred) return;
            _deferred = deferred;
            _camera.renderingPath = deferred ? RenderingPath.DeferredShading : RenderingPath.Forward;
            _camera.allowMSAA = !deferred && _keyed == null;
            _camera.backgroundColor = _keyed != null ? Key : new Color(0f, 0f, 0f, 0f);
            _image.material = _keyed;
            _size = default; // a new texture (antialiasing differs)
            GenesisLog.Info("Preview", _name + " stage renders " + (deferred ? "deferred" : "forward") + (_keyed != null ? ", keyed" : ", raw alpha") + " (" + shaders + ")");
        }

        /// <summary>True when some material of the model has a deferred pass and no forward one; lists the shaders seen.</summary>
        private static bool NeedsDeferred(GameObject model, out string shaders)
        {
            bool deferred = false;
            var names = new HashSet<string>();
            if (model != null)
                foreach (var r in model.GetComponentsInChildren<Renderer>(true))
                    foreach (var m in r.sharedMaterials)
                    {
                        if (m == null || m.shader == null) continue;
                        bool forward = false, def = false;
                        for (int i = 0; i < m.passCount; i++)
                        {
                            string pass = m.GetPassName(i);
                            if (pass == "FORWARD") forward = true;
                            else if (pass == "DEFERRED") def = true;
                        }
                        if (def && !forward) { deferred = true; names.Add(m.shader.name + " [deferred only]"); }
                        else names.Add(m.shader.name);
                    }
            shaders = names.Count == 0 ? "no material" : string.Join(", ", names);
            return deferred;
        }

        /// <summary>Logs a problem the watchdog found, once per kind per stage (the report then names it).</summary>
        private void Problem(string what)
        {
            string kind = what.Length > 24 ? what.Substring(0, 24) : what;
            if (!_logged.Add(kind)) return;
            GenesisLog.Warn("Preview", _name + " stage: " + what);
        }

        private void OnDestroy()
        {
            Stages.Remove(this);
            if (_keyed != null) Object.Destroy(_keyed);
            if (_camera != null) _camera.targetTexture = null;
            if (_texture != null) { _texture.Release(); Object.Destroy(_texture); }
            if (_root != null) Object.Destroy(_root.gameObject);
        }

        /// <summary>
        /// Makes <paramref name="go"/> (an inactive copy: no Awake has run) a picture only: removes every
        /// component that is not a transform, a renderer, an animator, a LOD group, a particle system, a
        /// light or one <paramref name="keep"/> allows; puts it on the stage's layer. Run before the copy
        /// is ever activated, so nothing of the game (characters list, network, physics) sees it.
        /// </summary>
        public static void Strip(GameObject go, System.Func<Component, bool> keep = null)
        {
            var all = go.GetComponentsInChildren<Component>(true);
            // Several passes: a component another one requires can only go after it.
            for (int pass = 0; pass < 4; pass++)
            {
                bool left = false;
                foreach (var c in all)
                {
                    if (c == null || Visual(c) || (keep != null && keep(c))) continue;
                    Object.DestroyImmediate(c);
                    if (c != null) left = true;
                }
                if (!left) break;
            }
            foreach (var c in go.GetComponentsInChildren<Component>(true))
                if (c != null && !Visual(c) && (keep == null || !keep(c)))
                    throw new System.InvalidOperationException("Preview still contains nonvisual component " + c.GetType().FullName);
            Restage(go);
        }

        /// <summary>Puts <paramref name="go"/> and everything under it on the stage's layer (lights: stage only, no shadows).</summary>
        public static void Restage(GameObject go)
        {
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = _layer;
            foreach (var l in go.GetComponentsInChildren<Light>(true)) { l.cullingMask = 1 << _layer; l.shadows = LightShadows.None; l.enabled = false; }
            foreach (var r in go.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // Animator and ParticleSystem live in Unity modules the plugin does not reference: by name.
        private static bool Visual(Component c) =>
            c is Transform || c is Renderer || c is MeshFilter || c is LODGroup || c is Light ||
            IsType(c, "UnityEngine.Animator") || IsType(c, "UnityEngine.ParticleSystem");

        internal static bool IsType(Component c, string fullName) => c.GetType().FullName == fullName;

        /// <summary>
        /// A copy's animators: no root motion (it stands still on the stage) and always animated. Set by
        /// name (see <see cref="Visual"/>); a missing property is skipped.
        /// </summary>
        public static void StillAnimators(GameObject go)
        {
            foreach (var c in go.GetComponentsInChildren<Behaviour>(true))
            {
                if (!IsType(c, "UnityEngine.Animator")) continue;
                var type = c.GetType();
                type.GetProperty("applyRootMotion")?.SetValue(c, false, null);
                type.GetProperty("fireEvents")?.SetValue(c, false, null);
                var culling = type.GetProperty("cullingMode");
                if (culling != null) culling.SetValue(c, System.Enum.ToObject(culling.PropertyType, 0), null); // AlwaysAnimate
            }
        }
    }
}
