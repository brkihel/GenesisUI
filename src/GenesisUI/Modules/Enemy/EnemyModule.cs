using System.Collections.Generic;
using BepInEx.Configuration;
using GenesisUI.Foundation;
using GenesisUI.Foundation.Contracts;
using GenesisUI.Host;
using GenesisUI.Theme;
using UnityEngine;

namespace GenesisUI.Modules.Enemy
{
    /// <summary>
    /// Plates over regular creatures, in the boss plate's language at a small size.
    ///
    /// Mirrors vanilla instead of re-implementing it (docs/regions.md, hud.enemy): vanilla decides
    /// which creatures get a plate, where it sits on screen, when it hides and what it says. Each
    /// vanilla plate is veiled and ours is placed inside it, so vanilla's own positioning and
    /// show/hide carry it with no lag, and it is destroyed together with vanilla's. Our plate
    /// ignores the veil through its own CanvasGroup. Bosses, other players and the ridden mount
    /// keep their own HUDs (hud.boss, vanilla).
    /// </summary>
    [GameContract("assembly_valheim", "EnemyHud", "instance")]
    [GameContract("assembly_valheim", "EnemyHud", "m_hudRoot")]
    [GameContract("assembly_valheim", "EnemyHud", "m_baseHud")]
    [GameContract("assembly_guiutils", "GuiBar", "GetSmoothValue")]
    internal sealed class EnemyModule : IUiModule
    {
        private const float PruneSeconds = 1f;

        private static readonly string[] OwnedRegions = { "hud.enemy" };

        private readonly ConfigEntry<int> _offsetY;
        private readonly Dictionary<GameObject, EnemyPlateView> _views = new Dictionary<GameObject, EnemyPlateView>();
        private readonly HashSet<GameObject> _others = new HashSet<GameObject>();
        private readonly List<GameObject> _gone = new List<GameObject>();
        private ThemeRuntime _theme;
        private string _cloneName;
        private float _prune;

        public EnemyModule(ConfigFile config)
        {
            _offsetY = config.Bind("Enemy", "OffsetY", 0,
                new ConfigDescription("Sobe (positivo) ou desce (negativo) as placas das criaturas em relação à posição do jogo.",
                    new AcceptableValueRange<int>(-200, 200)));
        }

        public string Id => "hud.enemy";
        public string NameToken => "$genesisui_module_enemy";
        public IReadOnlyList<string> Regions => OwnedRegions;
        public float RefreshRate => 0f; // vanilla repositions its plates every frame

        public void Build(ModuleContext context)
        {
            _theme = context.Theme;
            _cloneName = null;
            _views.Clear();
            _others.Clear();
        }

        public void Refresh(float deltaSeconds)
        {
            var hud = EnemyHud.instance;
            if (hud == null || hud.m_hudRoot == null) return;
            if (_cloneName == null) _cloneName = RegionRegistry.CloneName(hud.m_baseHud);
            if (_cloneName == null) return;

            // New vanilla plates: veil them and put ours inside. Anything else under the root
            // (boss, player, mount HUDs) is remembered so its name is read only once.
            var root = hud.m_hudRoot.transform;
            for (int i = 0; i < root.childCount; i++)
            {
                var go = root.GetChild(i).gameObject;
                if (_views.ContainsKey(go) || _others.Contains(go)) continue;
                if (go.name != _cloneName) { _others.Add(go); continue; }
                if (!VanillaVeil.IsVeiled(go)) VanillaVeil.Apply("module:" + Id, "hud.enemy/" + go.name, go, quiet: true);
                _views.Add(go, new EnemyPlateView(go.transform, _theme));
            }

            int offset = _offsetY.Value;
            foreach (var pair in _views)
            {
                if (pair.Key == null || !pair.Key.activeInHierarchy) continue;
                pair.Value.Apply(offset);
            }

            // Vanilla destroys plates of creatures that died or went away; forget them.
            _prune -= deltaSeconds;
            if (_prune > 0f) return;
            _prune = PruneSeconds;
            _gone.Clear();
            foreach (var pair in _views) if (pair.Key == null) _gone.Add(pair.Key);
            foreach (var go in _others) if (go == null) _gone.Add(go);
            for (int i = 0; i < _gone.Count; i++) { _views.Remove(_gone[i]); _others.Remove(_gone[i]); }
        }

        public void Teardown()
        {
            foreach (var pair in _views) pair.Value.Destroy();
            _views.Clear();
            _others.Clear();
            _theme = null;
        }
    }
}
