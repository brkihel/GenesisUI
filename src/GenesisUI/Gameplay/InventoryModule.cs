using System.Collections.Generic;
using GenesisUI.Foundation;
using GenesisUI.Foundation.Contracts;
using GenesisUI.Host;
using GenesisUI.InventoryModel;
using HarmonyLib;
using UnityEngine;

namespace GenesisUI.Gameplay
{
    /// <summary>
    /// GenesisUI's inventory slots (docs/GAMEPLAY.md, step F4.2a): keeps the player's inventory in
    /// the layout the admin set — ordinary rows, then quick-use/utility, then equipment — moving
    /// items when that layout changes, and keeps quick/utility rows out of the custom window until
    /// their panels arrive (F4.2c). Equipment cells appear in F4.2b. The placement patches read <see cref="Current"/>; while this
    /// module is not active it is null and they do nothing.
    /// </summary>
    [GameContract("assembly_valheim", "Player", "m_localPlayer", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Static, ValueType = "Player")]
    [GameContract("assembly_valheim", "Player", "SetInventorySize")]
    [GameContract("assembly_valheim", "Player", "TryGetUniqueKeyValue")]
    [GameContract("assembly_valheim", "Player", "AddUniqueKeyValue")]
    [GameContract("assembly_valheim", "Humanoid", "GetInventory")]
    [GameContract("assembly_valheim", "Inventory", "GetAllItems")]
    [GameContract("assembly_valheim", "Inventory", "GetHeight")]
    [GameContract("assembly_valheim", "ItemDrop+ItemData", "m_gridPos", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "Vector2i")]
    [GameContract("assembly_valheim", "InventoryGui", "m_playerGrid", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "InventoryGrid")]
    [GameContract("assembly_valheim", "InventoryGui", "SetInventorySize")]
    [GameContract("assembly_valheim", "InventoryGrid", "m_elements", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Collections.Generic.List\u00601[[InventoryElement, assembly_valheim, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]]")]
    [GameContract("assembly_valheim", "InventoryElement", "get_Position")]
    [GenesisUI.Foundation.Contracts.ContractDependency(typeof(InventorySafety), typeof(SavedLayout), typeof(Patches.EquipmentPatches))]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "Character", "Message", Parameters = new string[] { "MessageHud\u002BMessageType", "System.String", "System.Int32", "UnityEngine.Sprite", "System.Boolean" }, Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Method, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Void")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "InventoryGui", "get_instance", Parameters = new string[] {  }, Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Method, Static = GenesisUI.Foundation.Contracts.ContractStatic.Static, ValueType = "InventoryGui")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "InventoryGui", "IsVisible", Parameters = new string[] {  }, Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Method, Static = GenesisUI.Foundation.Contracts.ContractStatic.Static, ValueType = "System.Boolean")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_utils", "Vector2i", "y", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Int32")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_utils", "Vector2i", "x", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Int32")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_guiutils", "Localization", "get_instance", Parameters = new string[] {  }, Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Method, Static = GenesisUI.Foundation.Contracts.ContractStatic.Static, ValueType = "Localization")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_guiutils", "Localization", "Localize", Parameters = new string[] { "System.String" }, Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Method, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.String")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "ItemDrop\u002BItemData", "m_stack", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Int32")]
    [GenesisUI.Foundation.Contracts.ContractDependency(typeof(GenesisUI.Gameplay.InventorySafety), typeof(GenesisUI.Gameplay.InventorySettings), typeof(GenesisUI.Host.ModuleContext), typeof(GenesisUI.Foundation.VanillaSkin), typeof(GenesisUI.Gameplay.SavedLayout), typeof(GenesisUI.InventoryModel.SlotLayout), typeof(GenesisUI.InventoryModel.LayoutChange), typeof(GenesisUI.Foundation.GenesisLog), typeof(GenesisUI.Gameplay.EquipmentRules))]
    [GameContract("assembly_valheim", "ItemDrop+ItemData", "m_dropPrefab", Kind = ContractMemberKind.Field, Static = ContractStatic.Instance, ValueType = "UnityEngine.GameObject")]
    [GameContract("assembly_valheim", "ItemDrop", "m_itemData", Kind = ContractMemberKind.Field, Static = ContractStatic.Instance, ValueType = "ItemDrop+ItemData")]
    [GameContract("assembly_valheim", "ItemDrop+ItemData", "m_shared", Kind = ContractMemberKind.Field, Static = ContractStatic.Instance, ValueType = "ItemDrop+ItemData+SharedData")]
    [GameContract("assembly_valheim", "ItemDrop+ItemData+SharedData", "m_maxStackSize", Kind = ContractMemberKind.Field, Static = ContractStatic.Instance, ValueType = "System.Int32")]
    [GameContract("assembly_valheim", "ObjectDB", "get_instance", Parameters = new string[0])]
    [GameContract("assembly_valheim", "ObjectDB", "GetItemPrefab", Parameters = new[] { "System.String" })]
    [GameContract("assembly_valheim", "Inventory", "GetItemAt", Parameters = new[] { "System.Int32", "System.Int32" })]
    [GameContract("assembly_valheim", "Inventory", "m_onChanged", Kind = ContractMemberKind.Field, Static = ContractStatic.Instance, ValueType = "System.Action")]
    [ContractDependency(typeof(PocketRules), typeof(EquipmentMove))]
    [GameContract("assembly_valheim", "Inventory", "GetWidth", Parameters = new string[0])]
    internal sealed class InventoryModule : IUiModule, IModulePrerequisites
    {
        private static readonly string[] NoRegions = new string[0];

        /// <summary>The layout in force for the local player, or null (module off, faulted or no player).</summary>
        internal static SlotLayout Current { get; private set; }

        private Player _appliedTo;
        private SlotLayout _wanted;
        private bool _dirty = true;
        private int _integrationMask = -1;
        private string _lastRefusal;
        private readonly List<ItemAt> _items = new List<ItemAt>(64);
        private readonly VanillaSkin _specialSkin = new VanillaSkin("module:inv.slots");
        private InventoryElement _firstSpecialElement;
        private static InventoryModule _active;
        private static bool _equipmentPanelVisible;
        private AccessTools.FieldRef<InventoryGrid, List<InventoryElement>> _elements;
        private ItemDrop.ItemData.SharedData _coinShared;
        private int _originalCoinLimit, _appliedCoinLimit;
        private bool _coinOverrideLost;
        private static readonly System.Func<ItemDrop.ItemData, int> ReadCount = item => item.m_stack;
        private static readonly System.Func<ItemDrop.ItemData, (int, int)> ReadPosition = item => (item.m_gridPos.x, item.m_gridPos.y);

        public string Id => "inv.slots";
        public string NameToken => "$genesisui_module_inventory";
        public IReadOnlyList<string> Regions => NoRegions;
        public float RefreshRate => 10f;
        public string UnsupportedReason => InventorySafety.MissingPatches;

        public void Build(ModuleContext context)
        {
            InventorySafety.Resolve();
            // Resolved here, after the host checked the contracts (AGENTS.md §2a).
            _elements = AccessTools.FieldRefAccess<InventoryGrid, List<InventoryElement>>("m_elements");
            InventorySettings.Changed += OnSettingsChanged;
            context.OnRelease(() => InventorySettings.Changed -= OnSettingsChanged);
            _appliedTo = null;
            _dirty = true;
            _integrationMask = -1;
            _active = this;
            _equipmentPanelVisible = false;
        }

        public void Refresh(float deltaSeconds)
        {
            var player = Player.m_localPlayer;
            if (player == null)
            {
                Current = null;
                _appliedTo = null;
                return;
            }
            int mask = InventorySettings.IntegrationMask();
            if (mask != _integrationMask) { _integrationMask = mask; _dirty = true; }
            if (_dirty || _appliedTo != player) Apply(player);
            if (Current != null) { HideSpecialRows(); EquipmentRules.ReconcileExisting(player); ReconcilePockets(player); }
        }

        public void Teardown()
        {
            InventorySettings.Changed -= OnSettingsChanged;
            Current = null;
            _appliedTo = null;
            _equipmentPanelVisible = false;
            _active = null;
            _specialSkin.Restore();
            _firstSpecialElement = null;
            RestoreCoinLimit();
        }

        private void OnSettingsChanged() => _dirty = true;

        private void ReconcilePockets(Player player)
        {
            if (Current.EquipmentPosition(EquipSlot.Wallet).X < 0) { RestoreCoinLimit(); return; }
            if (_coinShared != null && _appliedCoinLimit != InventorySettings.WalletCapacity.Value)
            {
                if (_coinShared.m_maxStackSize == _appliedCoinLimit)
                { _appliedCoinLimit = InventorySettings.WalletCapacity.Value; _coinShared.m_maxStackSize = _appliedCoinLimit; }
                else _coinOverrideLost = true;
            }
            if (_coinShared == null && ObjectDB.instance != null)
            {
                var prefab = ObjectDB.instance.GetItemPrefab("Coins");
                var drop = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
                if (drop != null && drop.m_itemData != null)
                {
                    _coinShared = drop.m_itemData.m_shared;
                    _originalCoinLimit = _coinShared.m_maxStackSize;
                    _appliedCoinLimit = InventorySettings.WalletCapacity.Value;
                    _coinShared.m_maxStackSize = _appliedCoinLimit;
                    GenesisLog.Info("Module:inv.slots", "wallet coin stack capacity: " + _appliedCoinLimit + "; weight and counts unchanged");
                }
            }
            if (_coinShared != null && _coinShared.m_maxStackSize != _appliedCoinLimit && !_coinOverrideLost)
            {
                _coinOverrideLost = true;
                GenesisLog.Warn("Module:inv.slots", "another mod changed the coin stack capacity; preserving its value instead of reapplying ours");
            }
            var inventory = player.GetInventory();
            var all = inventory.GetAllItems();
            for (int i = 0; i < all.Count; i++)
            {
                var item = all[i];
                if (!Current.IsOrdinary(item.m_gridPos.x, item.m_gridPos.y) || item.m_dropPrefab == null) continue;
                string name = item.m_dropPrefab.name;
                EquipSlot? slot = null;
                if (PocketRules.Accepts(EquipSlot.Wallet, name)) slot = EquipSlot.Wallet;
                else if (PocketRules.Accepts(EquipSlot.KeyOne, name))
                {
                    var first = Current.EquipmentPosition(EquipSlot.KeyOne);
                    slot = inventory.GetItemAt(first.X, first.Y) == null ? EquipSlot.KeyOne : EquipSlot.KeyTwo;
                }
                if (!slot.HasValue) continue;
                var target = Current.EquipmentPosition(slot.Value);
                if (target.X < 0 || inventory.GetItemAt(target.X, target.Y) != null) continue;
                var snapshot = new PositionSnapshot<ItemDrop.ItemData>(all, ReadCount, ReadPosition);
                var plan = EquipmentMove.Equip(Current, slot.Value, i, snapshot.Positions);
                if (!plan.Ok || plan.Moves.Count == 0) continue;
                try
                {
                    snapshot.Apply(all, plan.Moves, inventory.GetWidth(), inventory.GetHeight(), InventorySafety.WritePosition);
                    inventory.m_onChanged?.Invoke();
                    if (!snapshot.Matches(inventory.GetAllItems())) throw new System.InvalidOperationException("Pocket callback changed item identity/count");
                }
                catch { snapshot.RestoreRemaining(inventory.GetAllItems(), InventorySafety.WritePosition); throw; }
                GenesisLog.Info("Module:inv.slots", "reserved " + slot.Value + " without changing item identity/count");
            }
        }

        internal static bool WalletItem(ItemDrop.ItemData item) => WalletActive && ReferenceEquals(item.m_shared, _active._coinShared);
        internal static bool WalletActive => _active != null && _active._coinShared != null && _active._coinShared.m_maxStackSize == _active._appliedCoinLimit && !Guard.IsTripped(InventorySafety.Owner);

        private void RestoreCoinLimit()
        {
            if (_coinShared != null && _coinShared.m_maxStackSize == _appliedCoinLimit) _coinShared.m_maxStackSize = _originalCoinLimit;
            _coinShared = null; _coinOverrideLost = false;
        }

        /// <summary>
        /// Brings the player's inventory into the admin's layout. Plans first (LayoutChange): if the
        /// items do not fit the new layout, nothing moves, the previous layout stays and the player
        /// is told once.
        /// </summary>
        private void Apply(Player player)
        {
            _dirty = false;
            _wanted = InventorySettings.Layout();
            var inventory = player.GetInventory();
            // A character that never ran GenesisUI (or ran it with other settings) is planned from what
            // it saved; one with no saved layout is plain vanilla: 4 ordinary rows, nothing special.
            var from = _appliedTo == player && Current != null
                ? Current
                : SavedLayout.Read(player) ?? new SlotLayout(SlotLayout.MinRows, 0, 0, null);

            var all = inventory.GetAllItems();
            var snapshot = new PositionSnapshot<ItemDrop.ItemData>(all, item => item.m_stack, item => (item.m_gridPos.x, item.m_gridPos.y));
            var plan = LayoutChange.Compute(from, _wanted, snapshot.Positions);

            SlotLayout layout;
            if (plan.Ok)
            {
                layout = _wanted;
                if (plan.Moves.Count > 0)
                    GenesisLog.Info("Module:inv.slots", "layout " + Describe(from) + " -> " + Describe(layout) + ": planned " + plan.Moves.Count +
                        " move(s), " + plan.Displaced + " to the inventory; snapshot items " + all.Count);
                _lastRefusal = null;
            }
            else
            {
                // Keep what the character has; say why, once per refusal.
                layout = from;
                if (plan.Reason != null) throw new System.InvalidOperationException(plan.Reason);
                string why = "layout " + Describe(_wanted) + " refused: " + plan.Overflow + " item(s) would not fit; keeping " + Describe(from);
                if (why != _lastRefusal)
                {
                    _lastRefusal = why;
                    GenesisLog.Warn("Module:inv.slots", why);
                    player.Message(MessageHud.MessageType.Center,
                        Localize("$genesisui_inventory_refused").Replace("{0}", plan.Overflow.ToString()));
                }
            }

            var previous = Current;
            Current = layout;
            try
            {
                InventorySafety.Resize(player, layout, snapshot, plan.Ok ? plan.Moves : new List<Move>(), () => EquipmentRules.ReconcileExisting(player));
                _appliedTo = player;
            }
            catch { Current = previous; throw; }
            _specialSkin.Restore();
            _firstSpecialElement = null;
        }

        /// <summary>The custom equipment panel shows the real special-row cells. If it is not
        /// available, vanilla shows every row so no stored item becomes inaccessible.</summary>
        internal static void SetEquipmentPanelVisible(bool visible)
        {
            if (_equipmentPanelVisible == visible) return;
            _equipmentPanelVisible = visible;
            if (_active != null && Current != null) _active.HideSpecialRows();
        }

        /// <summary>
        /// The custom window shows equipment cells and hides quick/utility cells until F4.2c.
        /// When the custom window is off or faulted, show the whole vanilla grid as a safe fallback.
        /// </summary>
        private void HideSpecialRows()
        {
            var gui = InventoryGui.instance;
            if (gui == null || !InventoryGui.IsVisible() || gui.m_playerGrid == null) return;
            var elements = _elements(gui.m_playerGrid);
            if (elements == null) return;
            var first = elements.Count > 0 ? elements[0] : null;
            if (first != _firstSpecialElement)
            {
                _specialSkin.Restore();
                _firstSpecialElement = first;
            }
            for (int i = 0; i < elements.Count; i++)
            {
                var element = elements[i];
                if (element == null || element.Position.y < Current.Rows) continue;
                var pos = element.Position;
                bool show = !_equipmentPanelVisible || Current.EquipmentAt(pos.x, pos.y).HasValue;
                var group = _specialSkin.Group(element.gameObject);
                if (group.alpha != (show ? 1f : 0f)) group.alpha = show ? 1f : 0f;
                if (group.blocksRaycasts != show) group.blocksRaycasts = show;
                if (group.interactable != show) group.interactable = show;
            }
        }

        private static string Describe(SlotLayout l) => l.Rows + " rows, " + l.Quick + " quick, " + l.Utility + " utility, " + l.Equipment.Count + " worn";

        private static string Localize(string token) => Localization.instance != null ? Localization.instance.Localize(token) : token;
    }
}
