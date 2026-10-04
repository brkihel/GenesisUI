using System;
using System.Collections.Generic;
using System.Reflection;
using GenesisUI.Foundation;
using GenesisUI.Foundation.Contracts;
using GenesisUI.Host;
using GenesisUI.InventoryModel;

namespace GenesisUI.Adapters.HipLantern
{
    [GameContract("HipLantern", "HipLantern.LanternItem", "IsLanternItem", Parameters = new[] { "ItemDrop+ItemData" })]
    [GameContract("HipLantern", "HipLantern.HipLantern", "itemSlotUtility", Kind = ContractMemberKind.Field, Static = ContractStatic.Static, ValueType = "BepInEx.Configuration.ConfigEntry`1<System.Boolean>")]
    [ContractDependency(typeof(AdapterBindings), typeof(InventoryIntegrations), typeof(GenesisLog))]
    internal sealed class HipLanternAdapter : IUiModule, IModulePrerequisites
    {
        internal const string Guid = "shudnal.HipLantern", Version = "1.1.12";
        private Assembly _assembly;
        private Func<ItemDrop.ItemData, bool> _isLantern;
        private BepInEx.Configuration.ConfigEntry<bool> _utility;
        private InventoryIntegrations.Entry _entry;
        public string Id => "adapter.hiplantern";
        public string NameToken => "$genesisui_module_hiplantern";
        public IReadOnlyList<string> Regions => Array.Empty<string>();
        public float RefreshRate => 10f;
        public string UnsupportedReason => AdapterBindings.Check(GetType(), Guid, Version, out _assembly);
        public void Build(ModuleContext context)
        {
            _isLantern = AdapterBindings.Delegate<Func<ItemDrop.ItemData, bool>>(AdapterBindings.Method(_assembly,
                "HipLantern.LanternItem", "IsLanternItem", typeof(ItemDrop.ItemData)));
            _utility = (BepInEx.Configuration.ConfigEntry<bool>)AdapterBindings.Field(_assembly, "HipLantern.HipLantern", "itemSlotUtility").GetValue(null);
            _entry = InventoryIntegrations.Register(new InventoryIntegrations.Entry
            {
                Owner = context.Owner, Classify = item => !_utility.Value && _isLantern(item) ? EquipSlot.Lantern : (EquipSlot?)null,
                Enabled = slot => slot == EquipSlot.Lantern && !_utility.Value,
            });
            context.OnRelease(_entry.Dispose);
            GenesisLog.Info("Adapter:HipLantern", "bound " + Guid + " " + Version + "; native independent equipment slot");
        }
        public void Refresh(float deltaSeconds) { }
        public void Teardown() { if (_entry != null) _entry.Dispose(); _entry = null; }
    }
}
