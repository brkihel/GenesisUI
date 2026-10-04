using System;
using System.Collections.Generic;
using System.Reflection;
using GenesisUI.Foundation;
using GenesisUI.Foundation.Contracts;
using GenesisUI.Host;
using GenesisUI.InventoryModel;
using GenesisUI.Modules.Windows;
using UnityEngine;
using UnityEngine.UI;

namespace GenesisUI.Adapters.Jewelcrafting
{
    [GameContract("Jewelcrafting", "Jewelcrafting.Visual", "IsFingerItem", Parameters = new[] { "ItemDrop+ItemData" })]
    [GameContract("Jewelcrafting", "Jewelcrafting.Visual", "IsNeckItem", Parameters = new[] { "ItemDrop+ItemData" })]
    [GameContract("Jewelcrafting", "Jewelcrafting.Jewelcrafting", "ringSlot", Kind = ContractMemberKind.Field, Static = ContractStatic.Static, ValueType = "BepInEx.Configuration.ConfigEntry`1<Jewelcrafting.Jewelcrafting+Toggle>")]
    [GameContract("Jewelcrafting", "Jewelcrafting.Jewelcrafting", "necklaceSlot", Kind = ContractMemberKind.Field, Static = ContractStatic.Static, ValueType = "BepInEx.Configuration.ConfigEntry`1<Jewelcrafting.Jewelcrafting+Toggle>")]
    [GameContract("Jewelcrafting", "Jewelcrafting.Jewelcrafting", "inventorySocketing", Kind = ContractMemberKind.Field, Static = ContractStatic.Static, ValueType = "BepInEx.Configuration.ConfigEntry`1<Jewelcrafting.Jewelcrafting+Toggle>")]
    [GameContract("Jewelcrafting", "Jewelcrafting.Jewelcrafting", "inventoryInteractBehaviour", Kind = ContractMemberKind.Field, Static = ContractStatic.Static, ValueType = "BepInEx.Configuration.ConfigEntry`1<Jewelcrafting.Jewelcrafting+InteractBehaviour>")]
    [GameContract("Jewelcrafting", "Jewelcrafting.GemStones+AddSocketAddingTab", "tab", Kind = ContractMemberKind.Field, Static = ContractStatic.Static, ValueType = "UnityEngine.Transform")]
    [GameContract("Jewelcrafting", "Jewelcrafting.GemStones+AddSocketIcons", "socketingButton", Kind = ContractMemberKind.Field, Static = ContractStatic.Static, ValueType = "UnityEngine.UI.Button")]
    [GameContract("Jewelcrafting", "Jewelcrafting.GemStones+AddFakeSocketsContainer", "openInventory", Kind = ContractMemberKind.Field, Static = ContractStatic.Static, ValueType = "Inventory")]
    [GameContract("Jewelcrafting", "Jewelcrafting.GemStones+OpenFakeSocketsContainer", "Open", Parameters = new[] { "InventoryGui", "ItemDrop+ItemData" })]
    [GameContract("Jewelcrafting", "ItemDataManager.ItemExtensions", "Data", Parameters = new[] { "ItemDrop+ItemData" })]
    [GameContract("Jewelcrafting", "ItemDataManager.ItemInfo", "Get", Parameters = new[] { "System.String" })]
    [GameContract("Jewelcrafting", "Jewelcrafting.ItemContainer", "boxSealed", Kind = ContractMemberKind.Field, Static = ContractStatic.Instance, ValueType = "System.Boolean")]
    [GameContract("assembly_valheim", "InventoryGui", "get_instance", Parameters = new string[0])]
    [GameContract("assembly_valheim", "InventoryGui", "OnTabCraftPressed", Parameters = new string[0])]
    [GameContract("assembly_valheim", "Player", "m_localPlayer", Kind = ContractMemberKind.Field, Static = ContractStatic.Static, ValueType = "Player")]
    [GameContract("assembly_valheim", "Player", "GetCurrentCraftingStation", Parameters = new string[0])]
    [ContractDependency(typeof(AdapterBindings), typeof(InventoryIntegrations), typeof(GenesisLog), typeof(Guard))]
    internal sealed class JewelcraftingAdapter : IUiModule, IModulePrerequisites
    {
        internal const string Guid = "org.bepinex.plugins.jewelcrafting", Version = "2.0.10";
        private static JewelcraftingAdapter _live;
        private Assembly _assembly;
        private Func<ItemDrop.ItemData, bool> _ring, _neck;
        private Func<bool> _ringOn, _neckOn, _inventorySockets, _interactEnabled;
        private Func<ItemDrop.ItemData, object> _container;
        private Func<object, bool> _sealed;
        private Func<InventoryGui, ItemDrop.ItemData, bool> _open;
        private FieldInfo _tab, _socketButton, _openInventory;
        private Button _nativeTab, _nativeSocketButton;
        private InventoryIntegrations.Entry _entry;
        private Action _pressTab, _pressCraft, _pressSockets;
        internal static bool Available { get; private set; }
        internal static bool SocketMode { get; private set; }
        internal static bool CanEditSelected { get; private set; }
        internal static Inventory OpenInventory => _live != null && _live._entry != null && !Guard.IsTripped(_live._entry.Owner)
            ? _live._entry.Container : null;
        public string Id => "adapter.jewelcrafting";
        public string NameToken => "$genesisui_module_jewelcrafting";
        public IReadOnlyList<string> Regions => Array.Empty<string>();
        public float RefreshRate => 0f;
        public string UnsupportedReason => AdapterBindings.Check(GetType(), Guid, Version, out _assembly);
        public void Build(ModuleContext context)
        {
            _ring = AdapterBindings.Delegate<Func<ItemDrop.ItemData, bool>>(AdapterBindings.Method(_assembly, "Jewelcrafting.Visual", "IsFingerItem", typeof(ItemDrop.ItemData)));
            _neck = AdapterBindings.Delegate<Func<ItemDrop.ItemData, bool>>(AdapterBindings.Method(_assembly, "Jewelcrafting.Visual", "IsNeckItem", typeof(ItemDrop.ItemData)));
            _ringOn = AdapterBindings.Toggle(AdapterBindings.Field(_assembly, "Jewelcrafting.Jewelcrafting", "ringSlot"), "On");
            _neckOn = AdapterBindings.Toggle(AdapterBindings.Field(_assembly, "Jewelcrafting.Jewelcrafting", "necklaceSlot"), "On");
            _inventorySockets = AdapterBindings.Toggle(AdapterBindings.Field(_assembly, "Jewelcrafting.Jewelcrafting", "inventorySocketing"), "On");
            _interactEnabled = AdapterBindings.Toggle(AdapterBindings.Field(_assembly, "Jewelcrafting.Jewelcrafting", "inventoryInteractBehaviour"), "Enabled");
            _container = AdapterBindings.ItemContainer(_assembly, "Jewelcrafting.ItemContainer");
            _sealed = AdapterBindings.ReadField<bool>(AdapterBindings.Field(_assembly, "Jewelcrafting.ItemContainer", "boxSealed"));
            _tab = AdapterBindings.Field(_assembly, "Jewelcrafting.GemStones+AddSocketAddingTab", "tab");
            _socketButton = AdapterBindings.Field(_assembly, "Jewelcrafting.GemStones+AddSocketIcons", "socketingButton");
            _openInventory = AdapterBindings.Field(_assembly, "Jewelcrafting.GemStones+AddFakeSocketsContainer", "openInventory");
            _open = AdapterBindings.Delegate<Func<InventoryGui, ItemDrop.ItemData, bool>>(AdapterBindings.Method(_assembly,
                "Jewelcrafting.GemStones+OpenFakeSocketsContainer", "Open", typeof(InventoryGui), typeof(ItemDrop.ItemData)));
            _pressTab = () => { if (_nativeTab != null && Available) _nativeTab.onClick.Invoke(); Refresh(0f); };
            _pressCraft = () => { var gui = InventoryGui.instance; if (gui != null) gui.OnTabCraftPressed(); Refresh(0f); };
            _pressSockets = () => { if (_nativeSocketButton != null && CanEditSelected) _nativeSocketButton.onClick.Invoke(); Refresh(0f); };
            _entry = InventoryIntegrations.Register(new InventoryIntegrations.Entry
            {
                Owner = context.Owner, Classify = Classify,
                Enabled = slot => slot == EquipSlot.Ring ? _ringOn() : slot == EquipSlot.Amulet && _neckOn(),
                CanOpen = CanOpen, Open = Open,
            });
            context.OnRelease(_entry.Dispose); _live = this;
            GenesisLog.Info("Adapter:Jewelcrafting", "bound " + Guid + " " + Version + "; jewelry, native Socket tab and gem-container callbacks");
        }
        private EquipSlot? Classify(ItemDrop.ItemData item) => _ring(item) ? EquipSlot.Ring : _neck(item) ? EquipSlot.Amulet : (EquipSlot?)null;
        private bool CanOpen(ItemDrop.ItemData item)
        {
            if (item == null || _interactEnabled()) return false;
            var player = Player.m_localPlayer;
            // The native tab's availability is the mod's authoritative station/config test.
            if (!_inventorySockets() && !Available) return false;
            var container = _container(item);
            return player != null && container != null && !_sealed(container);
        }
        private void Open(ItemDrop.ItemData item)
        {
            var gui = InventoryGui.instance;
            if (gui != null && CanOpen(item)) { _open(gui, item); Refresh(0f); }
        }
        internal static void PressTab() { var live = _live; if (live != null) Guard.Run(live._entry.Owner, live._pressTab); }
        internal static void PressCraft() { var live = _live; if (live != null) Guard.Run(live._entry.Owner, live._pressCraft); }
        internal static void PressSockets() { var live = _live; if (live != null) Guard.Run(live._entry.Owner, live._pressSockets); }
        public void Refresh(float deltaSeconds)
        {
            var tab = _tab.GetValue(null) as Transform;
            _nativeTab = tab != null ? tab.GetComponent<Button>() : null;
            _nativeSocketButton = _socketButton.GetValue(null) as Button;
            Available = _nativeTab != null && _nativeTab.gameObject.activeSelf;
            SocketMode = Available && !_nativeTab.interactable;
            CanEditSelected = SocketMode && _nativeSocketButton != null && _nativeSocketButton.gameObject.activeSelf && _nativeSocketButton.interactable;
            _entry.Container = _openInventory.GetValue(null) as Inventory;
        }
        public void Teardown()
        {
            if (_live == this) { _live = null; Available = SocketMode = CanEditSelected = false; }
            if (_entry != null) _entry.Dispose(); _entry = null; _nativeTab = _nativeSocketButton = null;
        }
    }
}
