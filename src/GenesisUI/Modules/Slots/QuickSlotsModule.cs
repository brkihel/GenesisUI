using System.Collections.Generic;
using BepInEx.Configuration;
using GenesisUI.Foundation.Contracts;
using GenesisUI.Gameplay;
using GenesisUI.Host;
using GenesisUI.InventoryModel;
using GenesisUI.Widgets;
using UnityEngine;

namespace GenesisUI.Modules.Slots
{
    /// <summary>
    /// The quick-use and action slots on the HUD (GAMEPLAY §1.4, D-037): the quick-use row (as many as
    /// the server allows, up to 4) where the food slots were, after the food and potion columns, and the
    /// smaller action row above it. Each cell mirrors its inventory slot (icon, amount, durability,
    /// equipped) and shows its hotkey; the hotkey uses the item exactly as vanilla's hotbar keys do
    /// (<c>Player.UseItem</c>), under vanilla's own conditions for taking input. The rows slide when the
    /// columns or the eitr bar change.
    /// </summary>
    [GameContract("assembly_valheim", "Humanoid", "UseItem")]
    [GameContract("assembly_valheim", "Humanoid", "GetInventory")]
    [GameContract("assembly_valheim", "Humanoid", "IsItemEquiped")]
    [GameContract("assembly_valheim", "Inventory", "GetItemAt")]
    [GameContract("assembly_valheim", "Character", "IsDead")]
    [GameContract("assembly_valheim", "Character", "InCutscene")]
    [GameContract("assembly_valheim", "Character", "IsTeleporting")]
    [GameContract("assembly_valheim", "Chat", "HasFocus")]
    [GameContract("assembly_valheim", "Console", "IsVisible")]
    [GameContract("assembly_valheim", "TextInput", "IsVisible")]
    [GameContract("assembly_valheim", "StoreGui", "IsVisible")]
    [GameContract("assembly_valheim", "InventoryGui", "IsVisible")]
    [GameContract("assembly_valheim", "Menu", "IsVisible")]
    [GameContract("assembly_valheim", "Minimap", "IsOpen")]
    [GameContract("assembly_valheim", "Player", "TakeInput", Parameters = new string[0])]
    [GenesisUI.Foundation.Contracts.ContractDependency(typeof(Patches.ShortcutInputPatch))]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "Player", "m_localPlayer", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Static, ValueType = "Player")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "ItemDrop\u002BItemData", "GetIcon", Parameters = new string[] {  }, Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Method, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "UnityEngine.Sprite")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "ItemDrop\u002BItemData", "m_shared", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "ItemDrop\u002BItemData\u002BSharedData")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "ItemDrop\u002BItemData\u002BSharedData", "m_maxStackSize", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Int32")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "ItemDrop\u002BItemData", "m_stack", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Int32")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "ItemDrop\u002BItemData\u002BSharedData", "m_useDurability", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Boolean")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "ItemDrop\u002BItemData", "m_durability", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Single")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "ItemDrop\u002BItemData", "GetMaxDurability", Parameters = new string[] {  }, Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Method, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Single")]
    [GenesisUI.Foundation.Contracts.ContractDependency(typeof(GenesisUI.Gameplay.SlotHotkeys), typeof(GenesisUI.Host.ModuleContext), typeof(GenesisUI.Widgets.Ui), typeof(GenesisUI.Theme.ThemeRuntime), typeof(GenesisUI.Widgets.SlotView), typeof(GenesisUI.Widgets.Glide), typeof(GenesisUI.InventoryModel.SlotLayout), typeof(GenesisUI.Widgets.HudAnchor), typeof(GenesisUI.Widgets.KeyText))]
    internal sealed class QuickSlotsModule : IUiModule
    {
        private const float QuickCell = 54f, ActionCell = 40f, Gap = 6f, RowGap = 8f, ColumnGap = 10f;
        private const float FlashSeconds = 0.18f;
        private static readonly string[] NoRegions = new string[0];

        private readonly SlotView[] _quick = new SlotView[SlotLayout.MaxQuick];
        private readonly SlotView[] _action = new SlotView[SlotLayout.MaxUtility];
        private readonly Glide[] _quickGlide = new Glide[SlotLayout.MaxQuick];
        private readonly Glide[] _actionGlide = new Glide[SlotLayout.MaxUtility];
        private readonly float[] _flash = new float[SlotHotkeys.Count];
        private readonly string[] _keyText = new string[SlotHotkeys.Count];
        private readonly KeyboardShortcut[] _keyShown = new KeyboardShortcut[SlotHotkeys.Count];
        private RectTransform _group;

        public string Id => "hud.slots";
        public string NameToken => "$genesisui_module_slots";
        public IReadOnlyList<string> Regions => NoRegions;
        public float RefreshRate => 0f; // every frame: the hotkeys

        public void Build(ModuleContext context)
        {
            SlotHotkeys.Resolve();
            _group = Ui.Place(Ui.Child(context.Root, "Slots"), Vector2.zero, Vector2.zero, new Vector2(10f, 10f));
            string frame = context.Theme.Sprite("hotslot") != null ? "hotslot" : "slot";
            for (int i = 0; i < _quick.Length; i++)
            {
                _quick[i] = new SlotView(_group, "Quick" + (i + 1), context.Theme, Vector2.zero, Vector2.zero, new Vector2(QuickCell, QuickCell), "", frame, "Food");
                _quickGlide[i] = Glide.On(_quick[i].Root);
            }
            for (int i = 0; i < _action.Length; i++)
            {
                _action[i] = new SlotView(_group, "Action" + (i + 1), context.Theme, Vector2.zero, Vector2.zero, new Vector2(ActionCell, ActionCell), "", frame, "Food");
                _actionGlide[i] = Glide.On(_action[i].Root);
            }
            for (int i = 0; i < _keyShown.Length; i++) _keyShown[i] = KeyboardShortcut.Empty;
            SlotHotkeys.Active = true;
        }

        public void Refresh(float deltaSeconds)
        {
            if (_group == null) return;
            var player = Player.m_localPlayer;
            var layout = InventoryModule.Current;
            bool show = player != null && !player.IsDead() && layout != null && (layout.Quick > 0 || layout.Utility > 0);
            if (_group.gameObject.activeSelf != show) _group.gameObject.SetActive(show);
            if (!show) return;

            var inventory = player.GetInventory();
            for (int i = 0; i < SlotHotkeys.Count; i++) if (_flash[i] > 0f) _flash[i] -= deltaSeconds;
            if (SlotHotkeys.TakesInput(player)) UseHotkeys(player, inventory, layout);

            float s = HudAnchor.Scale > 0f ? HudAnchor.Scale : 1f;
            if (!Mathf.Approximately(_group.localScale.x, s)) _group.localScale = new Vector3(s, s, 1f);
            float x = (HudAnchor.SlotsLeft + ColumnGap * s) / s;
            float y = HudAnchor.SlotsBottom / s;
            for (int i = 0; i < _quick.Length; i++)
            {
                bool on = i < layout.Quick;
                _quickGlide[i].To(new Vector2(x + i * (QuickCell + Gap), y), on ? 1f : 0f);
                if (on) Mirror(_quick[i], inventory, layout.QuickPosition(i), player, i);
            }
            // The action row sits on the quick-use row, centred over it.
            float quickWidth = Mathf.Max(1, layout.Quick) * (QuickCell + Gap) - Gap;
            float actionWidth = Mathf.Max(1, layout.Utility) * (ActionCell + Gap) - Gap;
            float ax = x + Mathf.Max(0f, (quickWidth - actionWidth) / 2f);
            float ay = y + (layout.Quick > 0 ? QuickCell + RowGap : 0f);
            for (int i = 0; i < _action.Length; i++)
            {
                bool on = i < layout.Utility;
                _actionGlide[i].To(new Vector2(ax + i * (ActionCell + Gap), ay), on ? 1f : 0f);
                if (on) Mirror(_action[i], inventory, layout.UtilityPosition(i), player, 4 + i);
            }
        }

        public void Teardown()
        {
            SlotHotkeys.Active = false;
            SlotHotkeys.ResetClaims();
            if (_group != null) Object.Destroy(_group.gameObject);
            _group = null;
        }

        private void UseHotkeys(Player player, Inventory inventory, SlotLayout layout)
        {
            for (int i = 0; i < SlotHotkeys.Count; i++)
            {
                bool quick = i < 4;
                int n = quick ? i : i - 4;
                if (n >= (quick ? layout.Quick : layout.Utility) || !SlotHotkeys.Down(i)) continue;
                var (x, y) = quick ? layout.QuickPosition(n) : layout.UtilityPosition(n);
                var item = inventory.GetItemAt(x, y);
                if (item == null) continue;
                // Exactly what vanilla's hotbar key does with its item.
                player.UseItem(null, item, false);
                _flash[i] = FlashSeconds;
            }
        }

        private void Mirror(SlotView cell, Inventory inventory, (int X, int Y) at, Player player, int hotkey)
        {
            var key = SlotHotkeys.Shortcut(hotkey);
            if (!key.Equals(_keyShown[hotkey]))
            {
                _keyShown[hotkey] = key;
                _keyText[hotkey] = KeyText.Short(key);
                cell.SetIndex(_keyText[hotkey]);
            }
            var item = inventory != null ? inventory.GetItemAt(at.X, at.Y) : null;
            if (item == null)
            {
                cell.SetIcon(null);
                cell.SetAmount(0);
                cell.SetBar(-1f);
                cell.SetState(_flash[hotkey] > 0f, false);
                return;
            }
            cell.SetIcon(item.GetIcon());
            cell.SetAmount(item.m_shared.m_maxStackSize > 1 ? item.m_stack : 0);
            float durability = item.m_shared.m_useDurability ? Mathf.Clamp01(item.m_durability / Mathf.Max(1f, item.GetMaxDurability())) : -1f;
            cell.SetBar(durability >= 0f && durability < 0.999f ? durability : -1f, durability >= 0f && durability < 0.25f);
            cell.SetState(_flash[hotkey] > 0f, player.IsItemEquiped(item));
        }
    }
}
