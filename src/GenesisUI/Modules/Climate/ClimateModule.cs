using System.Collections.Generic;
using GenesisUI.Foundation.Contracts;
using GenesisUI.Host;
using GenesisUI.Theme;
using GenesisUI.Widgets;
using UnityEngine;
using UnityEngine.UI;

namespace GenesisUI.Modules.Climate
{
    /// <summary>
    /// The interface feels the world (D-035). The gold frames (the metal shader) take a skin of the
    /// player's weather: frost creeping from the corners while Freezing (a little while Cold), drops
    /// running down while Wet, an ember glow while Burning, fine ash in the Ashlands; and the gold
    /// cools a few percent at night and warms at dusk. Below a quarter of health, the screen's edges
    /// glow like embers with each heartbeat, faster as health falls; while Rested a faint gold halo
    /// surrounds the vital bars. Reads only; every change eases in and out.
    /// </summary>
    [GameContract("assembly_valheim", "SEMan", "HaveStatusEffect", Parameters = new[] { "System.Int32" })]
    [GameContract("assembly_valheim", "SEMan", "s_statusEffectFreezing", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Static, ValueType = "System.Int32")]
    [GameContract("assembly_valheim", "SEMan", "s_statusEffectCold", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Static, ValueType = "System.Int32")]
    [GameContract("assembly_valheim", "SEMan", "s_statusEffectWet", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Static, ValueType = "System.Int32")]
    [GameContract("assembly_valheim", "SEMan", "s_statusEffectBurning", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Static, ValueType = "System.Int32")]
    [GameContract("assembly_valheim", "SEMan", "s_statusEffectRested", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Static, ValueType = "System.Int32")]
    [GameContract("assembly_valheim", "Player", "GetCurrentBiome")]
    [GameContract("assembly_valheim", "Character", "GetHealthPercentage")]
    [GameContract("assembly_valheim", "Character", "GetSEMan")]
    [GameContract("assembly_valheim", "Character", "IsDead")]
    [GameContract("assembly_valheim", "EnvMan", "GetDayFraction")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "Player", "m_localPlayer", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Static, ValueType = "Player")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "EnvMan", "get_instance", Parameters = new string[] {  }, Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Method, Static = GenesisUI.Foundation.Contracts.ContractStatic.Static, ValueType = "EnvMan")]
    [GenesisUI.Foundation.Contracts.ContractDependency(typeof(GenesisUI.Host.ModuleContext), typeof(GenesisUI.Theme.ThemeRuntime), typeof(GenesisUI.Widgets.Ui), typeof(GenesisUI.Widgets.EdgeLight))]
    internal sealed class ClimateModule : IUiModule
    {
        private const float LowHealth = 0.25f;
        private static readonly string[] NoRegions = new string[0];
        private static readonly int ClimateId = Shader.PropertyToID("_GenesisUIClimate");
        private static readonly int DayShiftId = Shader.PropertyToID("_GenesisUIDayShift");
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        private static readonly int BeatId = Shader.PropertyToID("_Beat");

        // Cool silver-blue at night, warm at dusk and dawn: a few percent, the style stays.
        private static readonly Vector4 Night = new Vector4(-0.07f, -0.03f, 0.06f, 0f);
        private static readonly Vector4 Dusk = new Vector4(0.06f, 0.0f, -0.08f, 0f);

        private ModuleContext _context;
        private Vector4 _climate, _shownClimate = new Vector4(-1f, 0f, 0f, 0f);
        private Vector4 _shift, _shownShift = new Vector4(-1f, 0f, 0f, 0f);
        private RawImage _heart;
        private Material _heartMaterial;
        private float _heartIntensity, _beatPhase, _shownHeart = -1f;
        internal static float RestedIntensity { get; private set; }

        public string Id => "hud.climate";
        public string NameToken => "$genesisui_module_climate";
        public IReadOnlyList<string> Regions => NoRegions;
        public float RefreshRate => 0f; // every frame: the heartbeat

        public void Build(ModuleContext context)
        {
            _context = context;
            _heartMaterial = context.Theme.NewLightMaterial("GenesisUI/Heartbeat");
            if (_heartMaterial != null)
            {
                var rt = Ui.Fill(Ui.Child(context.Root, "Heartbeat"));
                rt.SetAsFirstSibling(); // behind the HUD
                _heart = rt.gameObject.AddComponent<RawImage>();
                _heart.raycastTarget = false;
                _heart.material = _heartMaterial;
                _heart.enabled = false;
            }
            RestedIntensity = 0f;
            _climate = _shift = Vector4.zero;
            _heartIntensity = 0f;
        }

        public void Refresh(float deltaSeconds)
        {
            var player = Player.m_localPlayer;
            bool alive = player != null && !player.IsDead();
            float dt = Mathf.Min(deltaSeconds, 0.1f);

            // Weather on the metal: each part creeps in and out at its own pace.
            Vector4 target = Vector4.zero;
            bool rested = false;
            float health = 1f;
            if (alive)
            {
                var se = player.GetSEMan();
                target.x = se.HaveStatusEffect(SEMan.s_statusEffectFreezing) ? 1f : se.HaveStatusEffect(SEMan.s_statusEffectCold) ? 0.4f : 0f;
                target.y = se.HaveStatusEffect(SEMan.s_statusEffectWet) ? 1f : 0f;
                target.z = se.HaveStatusEffect(SEMan.s_statusEffectBurning) ? 1f : 0f;
                target.w = player.GetCurrentBiome() == Heightmap.Biome.AshLands ? 1f : 0f;
                rested = se.HaveStatusEffect(SEMan.s_statusEffectRested);
                health = player.GetHealthPercentage();
            }
            _climate.x = Ease(_climate.x, target.x, dt, 0.08f, 0.15f); // frost creeps slowly, melts a little faster
            _climate.y = Ease(_climate.y, target.y, dt, 0.6f, 0.12f);  // wet at once, dries slowly
            _climate.z = Ease(_climate.z, target.z, dt, 2f, 0.5f);
            _climate.w = Ease(_climate.w, target.w, dt, 0.2f, 0.2f);
            if ((_climate - _shownClimate).sqrMagnitude > 1e-6f) { _shownClimate = _climate; Shader.SetGlobalVector(ClimateId, _climate); }

            var shift = DayShift();
            _shift = Vector4.MoveTowards(_shift, shift, dt * 0.05f);
            if ((_shift - _shownShift).sqrMagnitude > 1e-7f) { _shownShift = _shift; Shader.SetGlobalVector(DayShiftId, _shift); }

            UpdateHeart(alive ? health : 1f, dt);

            RestedIntensity = Mathf.MoveTowards(RestedIntensity, rested && _context.Theme.LightsEnabled ? 1f : 0f, dt * 0.6f);
        }

        public void Teardown()
        {
            Shader.SetGlobalVector(ClimateId, Vector4.zero);
            Shader.SetGlobalVector(DayShiftId, Vector4.zero);
            _shownClimate = _shownShift = new Vector4(-1f, 0f, 0f, 0f);
            if (_heart != null) Object.Destroy(_heart.gameObject);
            if (_heartMaterial != null) Object.Destroy(_heartMaterial);
            _heart = null;
            _heartMaterial = null;
            RestedIntensity = 0f;
        }

        private static float Ease(float value, float target, float dt, float up, float down) =>
            Mathf.MoveTowards(value, target, dt * (target > value ? up : down));

        /// <summary>Night: cool; around sunrise and sunset: warm; day: none (EnvMan's day fraction: 0.5 is noon).</summary>
        private static Vector4 DayShift()
        {
            if (EnvMan.instance == null) return Vector4.zero;
            float f = EnvMan.instance.GetDayFraction();
            float night = 1f - Mathf.Clamp01(Mathf.InverseLerp(0.2f, 0.27f, f)) * Mathf.Clamp01(Mathf.InverseLerp(0.8f, 0.73f, f));
            float dusk = Bump(f, 0.25f, 0.05f) + Bump(f, 0.76f, 0.05f);
            return Night * night + Dusk * Mathf.Clamp01(dusk);
        }

        private static float Bump(float x, float centre, float width)
        {
            float d = (x - centre) / width;
            return Mathf.Exp(-d * d);
        }

        /// <summary>Below a quarter of health: an ember heartbeat at the screen's edges, faster as health falls.</summary>
        private void UpdateHeart(float health, float dt)
        {
            if (_heart == null) return;
            float low = health < LowHealth ? Mathf.InverseLerp(LowHealth, 0.04f, health) : 0f;
            float target = health < LowHealth ? Mathf.Lerp(0.35f, 1f, low) : 0f;
            _heartIntensity = Mathf.MoveTowards(_heartIntensity, target, dt * 0.8f);
            bool on = _heartIntensity > 0.001f;
            if (_heart.enabled != on) _heart.enabled = on;
            if (!on) { _beatPhase = 0f; return; }
            float bpm = Mathf.Lerp(62f, 130f, low);
            _beatPhase = Mathf.Repeat(_beatPhase + dt * bpm / 60f, 1f);
            // "Lub-dub": a strong beat and a softer one just after it.
            float a = (_beatPhase - 0.02f) / 0.05f, b = (_beatPhase - 0.2f) / 0.06f;
            float beat = Mathf.Clamp01(Mathf.Exp(-a * a) + 0.6f * Mathf.Exp(-b * b));
            _heartMaterial.SetFloat(BeatId, beat);
            if (!Mathf.Approximately(_heartIntensity, _shownHeart)) { _shownHeart = _heartIntensity; _heartMaterial.SetFloat(IntensityId, _heartIntensity); }
        }
    }
}
