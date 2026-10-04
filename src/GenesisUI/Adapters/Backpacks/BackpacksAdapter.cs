using System;
using System.Collections.Generic;
using System.Reflection;
using GenesisUI.Foundation;
using GenesisUI.Foundation.Contracts;
using GenesisUI.Host;
using GenesisUI.InventoryModel;

namespace GenesisUI.Adapters.Backpacks
{
    [GameContract("Backpacks", "ItemDataManager.ItemExtensions", "Data", Parameters = new[] { "ItemDrop+ItemData" })]
    [GameContract("Backpacks", "ItemDataManager.ItemInfo", "Get", Parameters = new[] { "System.String" })]
    [GameContract("Backpacks", "Backpacks.ItemContainer", "IsEquipable", Parameters = new string[0])]
    [GameContract("Backpacks", "Backpacks.ItemContainer", "AllowOpeningByKeypress", Parameters = new string[0])]
    [GameContract("Backpacks", "Backpacks.ItemContainer", "Inventory", Kind = ContractMemberKind.Field, Static = ContractStatic.Instance, ValueType = "Inventory")]
    [GameContract("Backpacks", "Backpacks.CustomContainer", "OpenContainer", Kind = ContractMemberKind.Field, Static = ContractStatic.Static, ValueType = "Backpacks.ItemContainer")]
    [GameContract("Backpacks", "Backpacks.CustomContainer+OpenFakeItemsContainer", "Open", Parameters = new[] { "InventoryGui", "ItemDrop+ItemData" })]
    [GameContract("assembly_valheim", "InventoryGui", "get_instance", Parameters = new string[0])]
    [ContractDependency(typeof(AdapterBindings), typeof(InventoryIntegrations), typeof(GenesisLog))]
    internal sealed class BackpacksAdapter : IUiModule, IModulePrerequisites
    {
        internal const string Guid = "org.bepinex.plugins.backpacks", Version = "1.3.10";
        private Assembly _assembly;
        private Func<ItemDrop.ItemData, object> _container;
        private Func<object, bool> _equipable, _allowOpen;
        private Func<object, Inventory> _inventory;
        private Func<InventoryGui, ItemDrop.ItemData, bool> _open;
        private FieldInfo _openContainer;
        private InventoryIntegrations.Entry _entry;
        public string Id => "adapter.backpacks";
        public string NameToken => "$genesisui_module_backpacks";
        public IReadOnlyList<string> Regions => Array.Empty<string>();
        public float RefreshRate => 0f;
        public string UnsupportedReason => AdapterBindings.Check(GetType(), Guid, Version, out _assembly);
        public void Build(ModuleContext context)
        {
            _container = AdapterBindings.ItemContainer(_assembly, "Backpacks.ItemContainer");
            _equipable = AdapterBindings.ReadMethod<bool>(AdapterBindings.Method(_assembly, "Backpacks.ItemContainer", "IsEquipable"));
            _allowOpen = AdapterBindings.ReadMethod<bool>(AdapterBindings.Method(_assembly, "Backpacks.ItemContainer", "AllowOpeningByKeypress"));
            _inventory = AdapterBindings.ReadField<Inventory>(AdapterBindings.Field(_assembly, "Backpacks.ItemContainer", "Inventory"));
            _openContainer = AdapterBindings.Field(_assembly, "Backpacks.CustomContainer", "OpenContainer");
            _open = AdapterBindings.Delegate<Func<InventoryGui, ItemDrop.ItemData, bool>>(AdapterBindings.Method(_assembly,
                "Backpacks.CustomContainer+OpenFakeItemsContainer", "Open", typeof(InventoryGui), typeof(ItemDrop.ItemData)));
            _entry = InventoryIntegrations.Register(new InventoryIntegrations.Entry
            {
                Owner = context.Owner, Classify = Classify, Enabled = slot => slot == EquipSlot.BackpackQuiver,
                CanOpen = CanOpen, Open = Open, BelowInventory = true,
            });
            context.OnRelease(_entry.Dispose);
            GenesisLog.Info("Adapter:Backpacks", "bound " + Guid + " " + Version + "; native container callbacks and key-open route");
        }
        private EquipSlot? Classify(ItemDrop.ItemData item)
        {
            var container = _container(item);
            return container != null && _equipable(container) ? EquipSlot.BackpackQuiver : (EquipSlot?)null;
        }
        private bool CanOpen(ItemDrop.ItemData item)
        {
            if (item == null) return false;
            var container = _container(item);
            return container != null && _allowOpen(container);
        }
        private void Open(ItemDrop.ItemData item)
        {
            var gui = InventoryGui.instance;
            if (gui == null || !CanOpen(item)) return;
            _open(gui, item); // the mod owns acceptance, open/close, subscriptions and persistence
            Refresh(0f);
            GenesisLog.Info("Adapter:Backpacks", "native item-container open command forwarded");
        }
        public void Refresh(float deltaSeconds)
        {
            var container = _openContainer.GetValue(null);
            _entry.Container = container != null ? _inventory(container) : null;
        }
        public void Teardown() { if (_entry != null) _entry.Dispose(); _entry = null; }
    }
}
