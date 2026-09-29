using System.Collections.Generic;
using BepInEx.Configuration;
using GenesisUI.Foundation.Contracts;
using GenesisUI.Host;
using GenesisUI.Motion;
using GenesisUI.Theme;
using GenesisUI.Widgets;
using HarmonyLib;
using UnityEngine;

namespace GenesisUI.Modules.Hotbar
{
    /// <summary>
    /// The eight hotbar slots on a framed plate at the bottom centre (concepts 3-6, 8, 10).
    /// Display only: keys 1-8 and the gamepad keep working through vanilla, because the
    /// vanilla HotkeyBar is veiled, not disabled, and its Update still runs.
    /// </summary>
    [GameContract("assembly_valheim", "Humanoid", "GetInventory")]
    [GameContract("assembly_valheim", "Inventory", "GetBoundItems")]
    [GameContract("assembly_valheim", "ItemDrop+ItemData", "m_gridPos")]
    [GameContract("assembly_valheim", "ItemDrop+ItemData", "m_stack")]
    [GameContract("assembly_valheim", "ItemDrop+ItemData", "m_equipped")]
    [GameContract("assembly_valheim", "ItemDrop+ItemData", "m_durability")]
    [GameContract("assembly_valheim", "ItemDrop+ItemData", "GetMaxDurability", Parameters = new string[0])]
    [GameContract("assembly_valheim", "ItemDrop+ItemData", "GetDurabilityPercentage")]
    [GameContract("assembly_valheim", "ItemDrop+ItemData", "GetIcon")]
    [GameContract("assembly_valheim", "ItemDrop+ItemData", "m_shared")]
    [GameContract("assembly_valheim", "ItemDrop+ItemData+SharedData", "m_maxStackSize")]
    [GameContract("assembly_valheim", "ItemDrop+ItemData+SharedData", "m_useDurability")]
    [GameContract("assembly_valheim", "HotkeyBar", "m_selected")]
    [GameContract("assembly_valheim", "Character", "IsDead")]
    [GameContract("assembly_utils", "ZInput", "IsGamepadActive")]
    [GameContract("assembly_valheim", "KeyHints", "instance")]
    internal sealed class HotbarModule : IUiModule
    {
        private const int SlotCount = 8;
        private const float SlotSize = 56f;
        private const float Gap = 8f;
        private const float SidePadding = 30f;
        private const float SlotGap = 6f;

        private static readonly string[] OwnedRegions = { "hud.hotbar" };

        private readonly ConfigEntry<int> _offsetY;
        private readonly ConfigEntry<float> _scale;
        private readonly ConfigEntry<int> _keyHintsLift;
        private readonly List<ItemDrop.ItemData> _bound = new List<ItemDrop.ItemData>(16);
        private readonly ItemDrop.ItemData[] _bySlot = new ItemDrop.ItemData[SlotCount];
        private readonly SlotView[] _slots = new SlotView[SlotCount];

        private RectTransform _plate;
        private HotkeyBar _vanillaBar;
        // Resolved in Build, after the host checked [GameContract]: a static initializer would
        // run at construction in Awake and take the whole plugin down if the field vanished.
        private AccessTools.FieldRef<HotkeyBar, int> _selected;
        private int _appliedOffset = int.MinValue;
        private float _appliedScale = float.NaN;

        public HotbarModule(ConfigFile config)
        {
            _offsetY = config.Bind("Hotbar", "OffsetY", 20,
                new ConfigDescription("Distância da barra de itens até a borda de baixo da tela, em pontos de interface.", new AcceptableValueRange<int>(0, 900)));
            _scale = config.Bind("Hotbar", "Scale", 1f,
                new ConfigDescription("Tamanho da barra de itens (1 = padrão).", new AcceptableValueRange<float>(0.5f, 2f)));
            _keyHintsLift = config.Bind("Hotbar", "KeyHintsLift", 76,
                new ConfigDescription("Quanto as dicas de atalho do jogo sobem para não ficar atrás da barra de itens (0 = não mexer).",
                    new AcceptableValueRange<int>(0, 600)));
        }

        public string Id => "hud.hotbar";
        public string NameToken => "$genesisui_module_hotbar";
        public IReadOnlyList<string> Regions => OwnedRegions;
        public float RefreshRate => 20f;

        public void Build(ModuleContext context)
        {
            var theme = context.Theme;
            // Diego's eight-cell bar is one fixed piece (D-027): drawn at its own proportions, the
            // cells sit where the art draws them (content area split by the declared gap).
            var drawn = theme.Size("hotbar_frame");
            // Diego's new eight-cell frame first (R-048 test); the single thin slots of 0.7.0-preview.4
            // (tag hotbar-minimal-v1) stay as the fallback.
            if (drawn.x > 0f)
            {
                _plate = Ui.Place(Ui.Child(context.Root, "Hotbar"), new Vector2(0.5f, 0f), Vector2.zero, drawn);
                Frame.Dress(_plate, theme, "hotbar_frame", "Hotbar");
                var c = theme.Content("hotbar_frame", new Vector4(32f, 12f, 32f, 12f));
                float gap = theme.Gap("hotbar_frame", 4f);
                var cell = new Vector2((drawn.x - c.x - c.z - (SlotCount - 1) * gap) / SlotCount, drawn.y - c.y - c.w);
                for (int i = 0; i < SlotCount; i++)
                {
                    var pos = new Vector2(c.x + i * (cell.x + gap), c.y);
                    _slots[i] = new SlotView(_plate, "Slot" + (i + 1), theme, Vector2.zero, pos, cell, (i + 1).ToString(), null, "Hotbar");
                }
            }
            else if (theme.Sprite("hotslot") != null)
            {
                // Eight single, thin slots with no plate around them (Diego, R-046: the eight-cell
                // plate was too heavy). Each slot carries its own frame and background.
                float width = SlotCount * SlotSize + (SlotCount - 1) * SlotGap;
                _plate = Ui.Place(Ui.Child(context.Root, "Hotbar"), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(width, SlotSize));
                for (int i = 0; i < SlotCount; i++)
                {
                    var pos = new Vector2(i * (SlotSize + SlotGap), 0f);
                    _slots[i] = new SlotView(_plate, "Slot" + (i + 1), theme, Vector2.zero, pos, new Vector2(SlotSize, SlotSize),
                                             (i + 1).ToString(), "hotslot", "Hotbar");
                }
            }
            else
            {
                float width = SlotCount * SlotSize + (SlotCount - 1) * Gap + 2 * SidePadding;
                _plate = Ui.Place(Ui.Child(context.Root, "Hotbar"), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(width, SlotSize + 24f));
                var plateSprite = theme.Sprite("plate");
                Ui.Image(Ui.Fill(Ui.Child(_plate, "Plate")), plateSprite, plateSprite != null ? Color.white : ThemeRuntime.ToUnity(theme.Tokens.PanelBackground));
                for (int i = 0; i < SlotCount; i++)
                {
                    var pos = new Vector2(SidePadding + i * (SlotSize + Gap), 12f);
                    _slots[i] = new SlotView(_plate, "Slot" + (i + 1), theme, Vector2.zero, pos, SlotSize, (i + 1).ToString());
                }
            }

            _selected = AccessTools.FieldRefAccess<HotkeyBar, int>("m_selected");

            // Vanilla's key hints sit along the bottom right, behind our wider hotbar (R-020). They
            // stay vanilla; they are only lifted, reversibly, above the plate.
            if (_keyHintsLift.Value > 0 && KeyHints.instance != null)
                Foundation.VanillaNudge.Apply("module:" + Id, "hud.keyHints", KeyHints.instance.transform as RectTransform,
                    new Vector2(0f, _keyHintsLift.Value));
            _vanillaBar = Hud.instance != null ? Hud.instance.GetComponentInChildren<HotkeyBar>(true) : null;
            ApplyLayout();
        }

        public void Refresh(float deltaSeconds)
        {
            if (_plate == null) return;
            ApplyLayout();

            var player = Player.m_localPlayer;
            bool show = player != null && !player.IsDead();
            if (_plate.gameObject.activeSelf != show) _plate.gameObject.SetActive(show);
            if (!show) return;

            for (int i = 0; i < SlotCount; i++) _bySlot[i] = null;
            _bound.Clear();
            player.GetInventory().GetBoundItems(_bound);
            foreach (var item in _bound)
            {
                int x = item.m_gridPos.x;
                if (x >= 0 && x < SlotCount) _bySlot[x] = item;
            }

            int gamepadSelected = _vanillaBar != null && ZInput.IsGamepadActive() ? _selected(_vanillaBar) : -1;
            float blink = Pulse.Evaluate(Time.unscaledTimeAsDouble, 0.5f);

            for (int i = 0; i < SlotCount; i++)
            {
                var slot = _slots[i];
                var item = _bySlot[i];
                if (item == null)
                {
                    slot.SetIcon(null);
                    slot.SetAmount(0);
                    slot.SetBar(-1f);
                    slot.SetState(i == gamepadSelected, false);
                    continue;
                }

                slot.SetIcon(item.GetIcon());
                slot.SetAmount(item.m_shared.m_maxStackSize > 1 ? item.m_stack : 0);

                bool worn = item.m_shared.m_useDurability && item.m_durability < item.GetMaxDurability();
                if (!worn) slot.SetBar(-1f);
                else if (item.m_durability <= 0f) slot.SetBar(blink > 0.5f ? 1f : -1f, danger: true); // broken: blinks red, like vanilla
                else slot.SetBar(item.GetDurabilityPercentage());

                slot.SetState(i == gamepadSelected, item.m_equipped);
            }
        }

        public void Teardown()
        {
            if (_plate != null) Object.Destroy(_plate.gameObject);
            _plate = null;
            _vanillaBar = null;
            _appliedOffset = int.MinValue;
            _appliedScale = float.NaN;
        }

        private void ApplyLayout()
        {
            if (_offsetY.Value != _appliedOffset)
            {
                _plate.anchoredPosition = new Vector2(0f, _offsetY.Value);
                _appliedOffset = _offsetY.Value;
            }
            if (_scale.Value != _appliedScale)
            {
                _plate.localScale = Vector3.one * _scale.Value;
                _appliedScale = _scale.Value;
            }
        }
    }
}
