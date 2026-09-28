using System.Collections.Generic;
using BepInEx.Configuration;
using GenesisUI.Foundation.Contracts;
using GenesisUI.Host;
using GenesisUI.HudModel;
using GenesisUI.Motion;
using GenesisUI.Widgets;
using UnityEngine;

namespace GenesisUI.Modules.Status
{
    /// <summary>
    /// Status effects as framed tiles with name and time (concepts 4-6, 8, 10). Any status
    /// effect a mod adds shows up here too: we read the same list vanilla reads.
    ///
    /// The guardian power is ONE tile (Diego, R-020): steady gold when ready; pulsing gold
    /// with the effect's time left while it is active (its effect tile is not repeated);
    /// dimmed with a small red cross and the cooldown once the effect ended.
    /// </summary>
    [GameContract("assembly_valheim", "Character", "GetSEMan")]
    [GameContract("assembly_valheim", "SEMan", "GetHUDStatusEffects")]
    [GameContract("assembly_valheim", "StatusEffect", "m_name")]
    [GameContract("assembly_valheim", "StatusEffect", "m_icon")]
    [GameContract("assembly_valheim", "StatusEffect", "m_flashIcon")]
    [GameContract("assembly_valheim", "StatusEffect", "m_cooldownIcon")]
    [GameContract("assembly_valheim", "StatusEffect", "m_hidden")]
    [GameContract("assembly_valheim", "StatusEffect", "GetIconText")]
    [GameContract("assembly_valheim", "Player", "GetGuardianPowerHUD")]
    [GameContract("assembly_valheim", "Character", "IsDead")]
    internal sealed class StatusModule : IUiModule
    {
        private const int MaxTiles = 24;

        private static readonly string[] OwnedRegions = { "hud.statusEffects", "hud.guardianPower" };

        private readonly ConfigEntry<int> _offsetX;
        private readonly ConfigEntry<int> _offsetY;
        private readonly ConfigEntry<int> _perRow;
        private readonly List<StatusEffect> _effects = new List<StatusEffect>(16);
        private readonly List<TileView> _tiles = new List<TileView>(MaxTiles);
        private RectTransform _group;
        private ModuleContext _context;
        private Vector2 _appliedOffset = new Vector2(float.NaN, float.NaN);

        public StatusModule(ConfigFile config)
        {
            _offsetX = config.Bind("Status", "OffsetX", 24,
                new ConfigDescription("Distância dos efeitos até a borda direita da tela.", new AcceptableValueRange<int>(0, 1800)));
            _offsetY = config.Bind("Status", "OffsetY", 300,
                new ConfigDescription("Distância dos efeitos até o topo da tela. O padrão deixa espaço para o minimapa do jogo.", new AcceptableValueRange<int>(0, 1000)));
            _perRow = config.Bind("Status", "PerRow", 6,
                new ConfigDescription("Quantos efeitos por linha antes de quebrar para a linha de baixo.", new AcceptableValueRange<int>(1, 12)));
        }

        public string Id => "hud.status";
        public string NameToken => "$genesisui_module_status";
        public IReadOnlyList<string> Regions => OwnedRegions;
        public float RefreshRate => 10f;

        public void Build(ModuleContext context)
        {
            _context = context;
            _group = Ui.Place(Ui.Child(context.Root, "Status"), new Vector2(1f, 1f), Vector2.zero, new Vector2(10f, 10f));
            _tiles.Clear();
            _appliedOffset = new Vector2(float.NaN, float.NaN);
        }

        public void Refresh(float deltaSeconds)
        {
            if (_group == null) return;
            var offset = new Vector2(-_offsetX.Value, -_offsetY.Value);
            if (offset != _appliedOffset) { _group.anchoredPosition = offset; _appliedOffset = offset; }

            var player = Player.m_localPlayer;
            bool show = player != null && !player.IsDead();
            if (_group.gameObject.activeSelf != show) _group.gameObject.SetActive(show);
            if (!show) return;

            float flash = Pulse.Evaluate(Time.unscaledTimeAsDouble, 0.4f);
            int used = 0;

            _effects.Clear();
            player.GetSEMan().GetHUDStatusEffects(_effects);
            player.GetGuardianPowerHUD(out StatusEffect power, out float cooldown);

            // The power's own effect, while it runs, is shown on the power tile instead of its own.
            StatusEffect activePower = null;
            if (power != null)
                foreach (var se in _effects)
                    if (se != null && se.m_name == power.m_name) { activePower = se; break; }

            if (power != null)
            {
                var tile = Tile(used++);
                tile.SetIcon(power.m_icon);
                tile.SetName(power.m_name);
                if (activePower != null)
                {
                    tile.SetTimeText(activePower.GetIconText());
                    tile.SetBadge(false);
                    tile.SetState(glow: 0.55f + 0.45f * Pulse.Evaluate(Time.unscaledTimeAsDouble, 1.5f), flash: 0f, dimmed: false);
                }
                else if (cooldown > 0f)
                {
                    tile.SetClock(TimeText.Clock(cooldown));
                    tile.SetBadge(true);
                    tile.SetState(glow: 0f, flash: 0f, dimmed: true);
                }
                else
                {
                    tile.SetTimeText(null);
                    tile.SetBadge(false);
                    tile.SetState(glow: 1f, flash: 0f, dimmed: false);
                }
            }

            foreach (var se in _effects)
            {
                if (se == null || se.m_hidden || se == activePower || used >= MaxTiles) continue;
                var tile = Tile(used++);
                tile.SetIcon(se.m_icon);
                tile.SetName(se.m_name);
                tile.SetTimeText(se.GetIconText());
                tile.SetBadge(false);
                tile.SetState(glow: 0f, flash: se.m_flashIcon ? flash : 0f, dimmed: se.m_cooldownIcon);
            }

            for (int i = 0; i < _tiles.Count; i++) _tiles[i].SetVisible(i < used);
        }

        public void Teardown()
        {
            if (_group != null) Object.Destroy(_group.gameObject);
            _group = null;
            _tiles.Clear();
        }

        private TileView Tile(int index)
        {
            while (_tiles.Count <= index) _tiles.Add(new TileView(_group, "Tile" + _tiles.Count, _context.Theme));
            var tile = _tiles[index];
            TileGrid.RightToLeft(index, _perRow.Value, TileView.CellWidth, TileView.CellHeight, out float x, out float y);
            tile.SetPosition(new Vector2(x, y));
            tile.SetVisible(true);
            return tile;
        }
    }
}
