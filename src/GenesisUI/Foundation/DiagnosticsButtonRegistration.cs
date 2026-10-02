using System.Collections;
using System.Reflection;
using Jotunn.Configs;
using Jotunn.Managers;
using GenesisUI.Foundation.Contracts;

namespace GenesisUI.Foundation
{
    // Jotunn 2.30.2 has no public removal API. Remove only entries still referencing our registration.
    [GameContract("Jotunn", "Jotunn.Managers.InputManager", "Buttons", Kind = ContractMemberKind.Field, Static = ContractStatic.Static, ValueType = "System.Collections.Generic.Dictionary`2<System.String,Jotunn.Configs.ButtonConfig>")]
    [GameContract("Jotunn", "Jotunn.Managers.InputManager", "ButtonToConfigDict", Kind = ContractMemberKind.Field, Static = ContractStatic.Static, ValueType = "System.Collections.Generic.Dictionary`2<BepInEx.Configuration.ConfigEntryBase,Jotunn.Configs.ButtonConfig>")]
    [GameContract("assembly_utils", "ZInput", "m_buttons", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Collections.Generic.Dictionary\u00602[[System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[ZInput\u002BButtonDef, assembly_utils, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]]")]
    [GameContract("assembly_utils", "ZInput", "instance")]
    internal sealed class DiagnosticsButtonRegistration
    {
        private readonly ButtonConfig _button;
        private readonly FieldInfo _buttons, _configs, _native;
        private readonly ZInput _input;
        private readonly object _definition;
        internal DiagnosticsButtonRegistration(ButtonConfig button)
        {
            _button = button;
            const BindingFlags flags = BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            _buttons = typeof(InputManager).GetField("Buttons", flags);
            _configs = typeof(InputManager).GetField("ButtonToConfigDict", flags);
            _native = typeof(ZInput).GetField("m_buttons", flags);
            _input = ZInput.instance;
            if (_input != null && _native.GetValue(_input) is IDictionary definitions) _definition = definitions[button.Name];
        }
        internal void Dispose()
        {
            if (_buttons.GetValue(null) is IDictionary buttons && ReferenceEquals(buttons[_button.Name], _button)) buttons.Remove(_button.Name);
            if (_configs.GetValue(null) is IDictionary configs && _button.ShortcutConfig != null && ReferenceEquals(configs[_button.ShortcutConfig], _button)) configs.Remove(_button.ShortcutConfig);
            if (_input != null && _definition != null && _native.GetValue(_input) is IDictionary definitions && ReferenceEquals(definitions[_button.Name], _definition)) definitions.Remove(_button.Name);
        }
    }
}
