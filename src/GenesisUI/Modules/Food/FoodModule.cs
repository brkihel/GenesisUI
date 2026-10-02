using System.Collections.Generic;
using GenesisUI.Foundation.Contracts;
using GenesisUI.Host;
using GenesisUI.HudModel;
using GenesisUI.Motion;
using GenesisUI.Widgets;
using UnityEngine;

namespace GenesisUI.Modules.Food
{
    /// <summary>
    /// What the player has eaten and drunk, as two vertical columns beside the vital bars (Diego,
    /// 2026-10-02; D-037): foods in the first, potion and mead effects in the second, each tile with its
    /// time left and a warm glow as it runs out. The columns follow the bars' right edge (it moves when
    /// the eitr bar comes and goes) and close up when one is empty; every move and fade is smooth. The
    /// quick-use and action slots line up after them (<see cref="HudAnchor.EffectsRight"/>).
    /// Potion effects are those a consumable item applies (<see cref="PotionEffects"/>); they leave the
    /// status tiles at the top right.
    /// </summary>
    [GameContract("assembly_valheim", "Player", "GetFoods")]
    [GameContract("assembly_valheim", "Player+Food", "m_item")]
    [GameContract("assembly_valheim", "Player+Food", "m_time")]
    [GameContract("assembly_valheim", "Player+Food", "CanEatAgain")]
    [GameContract("assembly_valheim", "Game", "m_foodRate")]
    [GameContract("assembly_valheim", "ItemDrop+ItemData", "GetIcon")]
    [GameContract("assembly_valheim", "ItemDrop+ItemData+SharedData", "m_foodBurnTime")]
    [GameContract("assembly_valheim", "Character", "IsDead")]
    [GameContract("assembly_valheim", "Character", "GetSEMan")]
    [GameContract("assembly_valheim", "SEMan", "GetStatusEffects")]
    [GameContract("assembly_valheim", "StatusEffect", "m_icon")]
    [GameContract("assembly_valheim", "StatusEffect", "m_ttl")]
    [GameContract("assembly_valheim", "StatusEffect", "GetRemaningTime")]
    internal sealed class FoodModule : IUiModule
    {
        private const int MaxFoods = 3, MaxPotions = 5;
        private const float Tile = 42f, Gap = 6f, ColumnGap = 8f;
        // A food's last minute and a potion's last ten seconds: a warm light that dims with what is left.
        private const float FoodEnding = 60f, PotionEnding = 10f;

        private static readonly string[] OwnedRegions = { "hud.food" };

        private readonly Column _foods = new Column(MaxFoods);
        private readonly Column _potions = new Column(MaxPotions);
        private readonly List<StatusEffect> _effects = new List<StatusEffect>(16);
        private RectTransform _group;

        private sealed class Column
        {
            public readonly SlotView[] Tiles;
            public readonly Glide[] Glides;
            public readonly EdgeLight[] Ending;
            public int Count;
            public Column(int max) { Tiles = new SlotView[max]; Glides = new Glide[max]; Ending = new EdgeLight[max]; }
        }

        public string Id => "hud.food";
        public string NameToken => "$genesisui_module_food";
        public IReadOnlyList<string> Regions => OwnedRegions;
        public float RefreshRate => 10f;

        public void Build(ModuleContext context)
        {
            _group = Ui.Place(Ui.Child(context.Root, "Effects"), Vector2.zero, Vector2.zero, new Vector2(10f, 10f));
            string frame = context.Theme.Sprite("hotslot") != null ? "hotslot" : "slot";
            Make(_foods, "Food", context, frame);
            Make(_potions, "Potion", context, frame);
            HudAnchor.EffectsValid = false;
        }

        private void Make(Column column, string name, ModuleContext context, string frame)
        {
            for (int i = 0; i < column.Tiles.Length; i++)
            {
                var tile = new SlotView(_group, name + (i + 1), context.Theme, Vector2.zero, Vector2.zero, new Vector2(Tile, Tile), null, frame, "Food");
                column.Tiles[i] = tile;
                column.Glides[i] = Glide.On(tile.Root);
                var light = EdgeLight.Create(_group, context.Theme, 9f);
                if (light != null)
                {
                    light.Target = tile.Root;
                    light.Pulse = 0.35f;
                    light.Speed = 1.5f;
                }
                column.Ending[i] = light;
            }
        }

        public void Refresh(float deltaSeconds)
        {
            if (_group == null) return;
            var player = Player.m_localPlayer;
            bool show = player != null && !player.IsDead();
            if (_group.gameObject.activeSelf != show) _group.gameObject.SetActive(show);
            if (!show) { HudAnchor.EffectsValid = false; return; }

            float blink = Pulse.Evaluate(Time.unscaledTimeAsDouble, 0.6f);
            ShowFoods(player, blink);
            ShowPotions(player, blink);
            Layout();
        }

        private void ShowFoods(Player player, float blink)
        {
            var foods = player.GetFoods();
            int n = foods != null ? Mathf.Min(foods.Count, MaxFoods) : 0;
            float time = Time.time;
            for (int i = 0; i < n; i++)
            {
                var food = foods[i];
                var tile = _foods.Tiles[i];
                // Vanilla pulses the icon when the food can be eaten again.
                float alpha = food.CanEatAgain() ? 0.7f + Mathf.Sin(time * 5f) * 0.3f : 1f;
                tile.SetIcon(food.m_item.GetIcon(), alpha);
                float left = Game.m_foodRate > 0f ? food.m_time / Game.m_foodRate : food.m_time;
                tile.SetTime(TimeText.Food(left), blink);
                float burn = food.m_item.m_shared.m_foodBurnTime;
                tile.SetBar(burn > 0f ? Mathf.Clamp01(food.m_time / burn) : -1f);
                Ending(_foods, i, left, FoodEnding);
            }
            for (int i = n; i < MaxFoods; i++) Ending(_foods, i, -1f, FoodEnding);
            _foods.Count = n;
        }

        private void ShowPotions(Player player, float blink)
        {
            _effects.Clear();
            var all = player.GetSEMan().GetStatusEffects();
            int n = 0;
            if (all != null)
                foreach (var se in all)
                {
                    if (n >= MaxPotions) break;
                    if (se == null || se.m_icon == null || !PotionEffects.Is(se)) continue;
                    var tile = _potions.Tiles[n];
                    tile.SetIcon(se.m_icon);
                    float left = se.m_ttl > 0f ? se.GetRemaningTime() : -1f;
                    if (left >= 0f) tile.SetTime(TimeText.Clock(left), blink);
                    else tile.ClearCorner();
                    tile.SetBar(se.m_ttl > 0f ? Mathf.Clamp01(left / se.m_ttl) : -1f);
                    Ending(_potions, n, left, Mathf.Min(PotionEnding, se.m_ttl * 0.3f));
                    n++;
                }
            for (int i = n; i < MaxPotions; i++) Ending(_potions, i, -1f, PotionEnding);
            _potions.Count = n;
        }

        private static void Ending(Column column, int i, float left, float window)
        {
            var light = column.Ending[i];
            if (light == null) return;
            light.Intensity = left >= 0f && left < window && window > 0f ? Mathf.Lerp(0.12f, 0.9f, left / window) : 0f;
        }

        /// <summary>
        /// The columns after the bars, bottom-aligned with them and stacked upwards; an empty column takes
        /// no room. Tiles glide to their places and fade in and out.
        /// </summary>
        private void Layout()
        {
            float s = HudAnchor.Scale > 0f ? HudAnchor.Scale : 1f;
            float x = (HudAnchor.Valid ? HudAnchor.Right : 160f) + ColumnGap * s;
            float bottom = HudAnchor.SlotsBottom;
            float step = (Tile + Gap) * s;
            if (!Mathf.Approximately(_group.localScale.x, s)) _group.localScale = new Vector3(s, s, 1f);
            float right = x - ColumnGap * s;
            foreach (var column in new[] { _foods, _potions })
            {
                for (int i = 0; i < column.Tiles.Length; i++)
                {
                    bool on = i < column.Count;
                    // In the group's (scaled) space.
                    var at = new Vector2(x / s, (bottom + i * step) / s);
                    if (!on) at.y = (bottom + Mathf.Max(0, column.Count) * step) / s; // a closing tile sinks where the stack ends
                    column.Glides[i].To(at, on ? 1f : 0f);
                }
                if (column.Count > 0)
                {
                    right = x + Tile * s;
                    x = right + ColumnGap * s;
                }
            }
            HudAnchor.EffectsRight = right;
            HudAnchor.EffectsValid = true;
        }

        public void Teardown()
        {
            HudAnchor.EffectsValid = false;
            if (_group != null) Object.Destroy(_group.gameObject);
            _group = null;
        }
    }

    /// <summary>
    /// Which status effects are a potion's or a mead's: those some consumable item applies when used
    /// (its <c>m_consumeStatusEffect</c>), read from the item database once it is loaded, modded potions
    /// included; re-read when the database grows.
    /// </summary>
    [GameContract("assembly_valheim", "ObjectDB", "m_items")]
    [GameContract("assembly_valheim", "ItemDrop+ItemData+SharedData", "m_consumeStatusEffect")]
    [GameContract("assembly_valheim", "StatusEffect", "NameHash")]
    internal static class PotionEffects
    {
        private static readonly HashSet<int> Hashes = new HashSet<int>();
        private static int _items = -1;

        internal static bool Is(StatusEffect se)
        {
            var db = ObjectDB.instance;
            if (db == null || db.m_items == null) return false;
            if (db.m_items.Count != _items) Read(db);
            return Hashes.Contains(se.NameHash());
        }

        private static void Read(ObjectDB db)
        {
            _items = db.m_items.Count;
            Hashes.Clear();
            foreach (var go in db.m_items)
            {
                if (go == null) continue;
                var drop = go.GetComponent<ItemDrop>();
                var effect = drop != null && drop.m_itemData != null && drop.m_itemData.m_shared != null ? drop.m_itemData.m_shared.m_consumeStatusEffect : null;
                if (effect != null) Hashes.Add(effect.NameHash());
            }
        }
    }
}
