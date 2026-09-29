using System.Collections.Generic;
using BepInEx.Configuration;
using GenesisUI.Foundation.Contracts;
using GenesisUI.Host;
using GenesisUI.HudModel;
using GenesisUI.Motion;
using GenesisUI.Widgets;
using UnityEngine;

namespace GenesisUI.Modules.Food
{
    /// <summary>
    /// The three food slots next to the vital bars (concepts 4-6, 8, 10): icon, time left
    /// written exactly as vanilla writes it, a bar of what is left, and the vanilla pulse
    /// when the food can be eaten again.
    /// </summary>
    [GameContract("assembly_valheim", "Player", "GetFoods")]
    [GameContract("assembly_valheim", "Player+Food", "m_item")]
    [GameContract("assembly_valheim", "Player+Food", "m_time")]
    [GameContract("assembly_valheim", "Player+Food", "CanEatAgain")]
    [GameContract("assembly_valheim", "Game", "m_foodRate")]
    [GameContract("assembly_valheim", "ItemDrop+ItemData", "GetIcon")]
    [GameContract("assembly_valheim", "ItemDrop+ItemData+SharedData", "m_foodBurnTime")]
    [GameContract("assembly_valheim", "Character", "IsDead")]
    internal sealed class FoodModule : IUiModule
    {
        private const int SlotCount = 3;
        private const float SlotSize = 58f;
        private const float Gap = 8f;

        private static readonly string[] OwnedRegions = { "hud.food" };

        private readonly ConfigEntry<int> _offsetX;
        private readonly ConfigEntry<int> _offsetY;
        private readonly SlotView[] _slots = new SlotView[SlotCount];
        private RectTransform _group;
        private Vector2 _appliedOffset = new Vector2(float.NaN, float.NaN);

        public FoodModule(ConfigFile config)
        {
            _offsetX = config.Bind("Food", "OffsetX", 172,
                new ConfigDescription("Distância dos espaços de comida até a borda esquerda da tela.", new AcceptableValueRange<int>(0, 1800)));
            _offsetY = config.Bind("Food", "OffsetY", 90,
                new ConfigDescription("Distância dos espaços de comida até a borda de baixo da tela.", new AcceptableValueRange<int>(0, 1000)));
        }

        public string Id => "hud.food";
        public string NameToken => "$genesisui_module_food";
        public IReadOnlyList<string> Regions => OwnedRegions;
        public float RefreshRate => 10f;

        public void Build(ModuleContext context)
        {
            _group = Ui.Place(Ui.Child(context.Root, "Food"), Vector2.zero, Vector2.zero,
                              new Vector2(SlotCount * SlotSize + (SlotCount - 1) * Gap, SlotSize));
            for (int i = 0; i < SlotCount; i++)
                _slots[i] = new SlotView(_group, "Food" + (i + 1), context.Theme, Vector2.zero, new Vector2(i * (SlotSize + Gap), 0f),
                                         new Vector2(SlotSize, SlotSize), null,
                                         context.Theme.Sprite("hotslot") != null ? "hotslot" : "slot", "Food");
            _appliedOffset = new Vector2(float.NaN, float.NaN);
        }

        public void Refresh(float deltaSeconds)
        {
            if (_group == null) return;
            var offset = new Vector2(_offsetX.Value, _offsetY.Value);
            if (offset != _appliedOffset) { _group.anchoredPosition = offset; _appliedOffset = offset; }

            var player = Player.m_localPlayer;
            bool show = player != null && !player.IsDead();
            if (_group.gameObject.activeSelf != show) _group.gameObject.SetActive(show);
            if (!show) return;

            var foods = player.GetFoods();
            float time = Time.time;
            float blink = Pulse.Evaluate(Time.unscaledTimeAsDouble, 0.6f);
            for (int i = 0; i < SlotCount; i++)
            {
                var slot = _slots[i];
                if (foods == null || i >= foods.Count)
                {
                    slot.SetIcon(null);
                    slot.ClearCorner();
                    slot.SetBar(-1f);
                    continue;
                }

                var food = foods[i];
                // Vanilla pulses the icon when the food can be eaten again.
                float alpha = food.CanEatAgain() ? 0.7f + Mathf.Sin(time * 5f) * 0.3f : 1f;
                slot.SetIcon(food.m_item.GetIcon(), alpha);
                slot.SetTime(TimeText.Food(Game.m_foodRate > 0f ? food.m_time / Game.m_foodRate : food.m_time), blink);
                float burn = food.m_item.m_shared.m_foodBurnTime;
                slot.SetBar(burn > 0f ? Mathf.Clamp01(food.m_time / burn) : -1f);
            }
        }

        public void Teardown()
        {
            if (_group != null) Object.Destroy(_group.gameObject);
            _group = null;
        }
    }
}
