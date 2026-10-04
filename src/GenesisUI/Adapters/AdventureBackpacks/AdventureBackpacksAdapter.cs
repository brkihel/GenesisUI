using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using HarmonyLib;
using GenesisUI.Foundation;
using GenesisUI.Foundation.Contracts;
using GenesisUI.Host;
using GenesisUI.InventoryModel;

namespace GenesisUI.Adapters.AdventureBackpacks
{
    [GameContract("AdventureBackpacks", "AdventureBackpacks.API.ABAPI", "IsBackpack", Parameters = new[] { "ItemDrop+ItemData" })]
    [GameContract("AdventureBackpacks", "AdventureBackpacks.API.ABAPI", "IsThisBackpackEquipped", Parameters = new[] { "Player", "ItemDrop+ItemData" })]
    [GameContract("AdventureBackpacks", "AdventureBackpacks.API.ABAPI", "CanOpenBackpack", Parameters = new[] { "Player" })]
    [GameContract("AdventureBackpacks", "AdventureBackpacks.API.ABAPI", "GetEquippedBackpack", Parameters = new[] { "Player" })]
    [GameContract("AdventureBackpacks", "AdventureBackpacks.API.ABAPI", "OpenBackpack", Parameters = new[] { "Player", "InventoryGui" })]
    [GameContract("AdventureBackpacks", "AdventureBackpacks.API.ABAPI+Backpack", "Inventory", Kind = ContractMemberKind.Field, Static = ContractStatic.Instance, ValueType = "Inventory")]
    [GameContract("assembly_valheim", "Player", "m_localPlayer", Kind = ContractMemberKind.Field, Static = ContractStatic.Static, ValueType = "Player")]
    [GameContract("assembly_valheim", "InventoryGui", "get_instance", Parameters = new string[0])]
    [GameContract("assembly_valheim", "InventoryGui", "m_containerGrid", Kind = ContractMemberKind.Field, Static = ContractStatic.Instance, ValueType = "InventoryGrid")]
    [GameContract("assembly_valheim", "InventoryGui", "IsContainerOpen", Parameters = new string[0])]
    [GameContract("assembly_valheim", "InventoryGrid", "GetInventory", Parameters = new string[0])]
    [ContractDependency(typeof(AdapterBindings), typeof(InventoryIntegrations), typeof(GenesisLog))]
    internal sealed class AdventureBackpacksAdapter : IUiModule, IModulePrerequisites
    {
        internal const string Guid = "vapok.mods.adventurebackpacks", Version = "2.0.3";
        private Assembly _assembly;
        private InventoryIntegrations.Entry _entry;
        private Func<ItemDrop.ItemData, bool> _isPack;
        private Func<Player, ItemDrop.ItemData, bool> _equipped;
        private Func<Player, bool> _canOpen;
        private Func<Player, Inventory> _inventory;
        private Action<Player, InventoryGui> _open;
        private AccessTools.FieldRef<InventoryGui, InventoryGrid> _grid;
        public string Id => "adapter.adventurebackpacks";
        public string NameToken => "$genesisui_module_adventure_backpacks";
        public IReadOnlyList<string> Regions => new string[0];
        public float RefreshRate => 0f;
        public string UnsupportedReason => AdapterBindings.Check(GetType(), Guid, Version, out _assembly);
        public void Build(ModuleContext context)
        {
            const string api = "AdventureBackpacks.API.ABAPI";
            _isPack = AdapterBindings.Delegate<Func<ItemDrop.ItemData, bool>>(AdapterBindings.Method(_assembly, api, "IsBackpack", typeof(ItemDrop.ItemData)));
            _equipped = AdapterBindings.Delegate<Func<Player, ItemDrop.ItemData, bool>>(AdapterBindings.Method(_assembly, api, "IsThisBackpackEquipped", typeof(Player), typeof(ItemDrop.ItemData)));
            _canOpen = AdapterBindings.Delegate<Func<Player, bool>>(AdapterBindings.Method(_assembly, api, "CanOpenBackpack", typeof(Player)));
            _open = AdapterBindings.Delegate<Action<Player, InventoryGui>>(AdapterBindings.Method(_assembly, api, "OpenBackpack", typeof(Player), typeof(InventoryGui)));
            var getter = AdapterBindings.Method(_assembly, api, "GetEquippedBackpack", typeof(Player));
            var player = Expression.Parameter(typeof(Player), "player");
            var pack = Expression.Variable(getter.ReturnType, "pack");
            var read = Expression.Block(new[] { pack }, Expression.Assign(pack, Expression.Call(getter, player)),
                Expression.Condition(Expression.Property(pack, "HasValue"),
                    Expression.Field(Expression.Property(pack, "Value"), "Inventory"), Expression.Constant(null, typeof(Inventory))));
            _inventory = Expression.Lambda<Func<Player, Inventory>>(read, player).Compile();
            _grid = AccessTools.FieldRefAccess<InventoryGui, InventoryGrid>("m_containerGrid");
            _entry = InventoryIntegrations.Register(new InventoryIntegrations.Entry { Owner = context.Owner, Classify = Classify, Enabled = Enabled, CanOpen = CanOpen, Open = Open });
            context.OnRelease(_entry.Dispose);
            GenesisLog.Info("Adapter:AdventureBackpacks", "bound public ABAPI " + Version + "; native equip/open restrictions retained");
        }
        private EquipSlot? Classify(ItemDrop.ItemData item) => _isPack(item) ? EquipSlot.BackpackQuiver : (EquipSlot?)null;
        private bool Enabled(EquipSlot slot) => slot == EquipSlot.BackpackQuiver;
        private bool CanOpen(ItemDrop.ItemData item)
        {
            var player = Player.m_localPlayer;
            return player != null && item != null && _isPack(item) && _equipped(player, item) && _canOpen(player);
        }
        private void Open(ItemDrop.ItemData item)
        {
            var gui = InventoryGui.instance;
            if (gui != null && CanOpen(item)) { _open(Player.m_localPlayer, gui); Refresh(0f); }
        }
        public void Refresh(float deltaSeconds)
        {
            var player = Player.m_localPlayer;
            var gui = InventoryGui.instance;
            var inventory = player != null ? _inventory(player) : null;
            var grid = gui != null ? _grid(gui) : null;
            _entry.Container = gui != null && gui.IsContainerOpen() && grid != null && ReferenceEquals(grid.GetInventory(), inventory) ? inventory : null;
            _entry.BelowInventory = true;
        }
        public void Teardown() { if (_entry != null) _entry.Dispose(); _entry = null; }
    }
}
