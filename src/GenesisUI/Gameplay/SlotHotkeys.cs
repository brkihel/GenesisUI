using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using GenesisUI.Foundation;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace GenesisUI.Gameplay
{
    /// <summary>
    /// The hotkeys of the quick-use and action (utility) slots (GAMEPLAY §1.4, D-037): the player's own,
    /// never synced, any key with any modifiers (Alt+Z, Ctrl+1, F2...). While one of them is held with
    /// exactly its modifiers, vanilla's buttons bound to the same key read as not pressed
    /// (<see cref="Patches.ShortcutInputPatch"/>): Alt+X uses an action slot and does not make the
    /// character sit; X alone still sits. Checked once per frame, allocation-free.
    /// </summary>
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "Character", "IsDead", Parameters = new string[] {  }, Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Method, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Boolean")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "Character", "InCutscene", Parameters = new string[] {  }, Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Method, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Boolean")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "Character", "IsTeleporting", Parameters = new string[] {  }, Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Method, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Boolean")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "Player", "m_localPlayer", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Static, ValueType = "Player")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_utils", "ZInput", "get_instance", Parameters = new string[] {  }, Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Method, Static = GenesisUI.Foundation.Contracts.ContractStatic.Static, ValueType = "ZInput")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_utils", "ZInput\u002BButtonDef", "get_ButtonAction", Parameters = new string[] {  }, Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Method, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "UnityEngine.InputSystem.InputAction")]
    [GenesisUI.Foundation.Contracts.ContractDependency(typeof(GenesisUI.Foundation.Guard), typeof(GenesisUI.Patches.TextInputFocus), typeof(GenesisUI.InventoryModel.SlotLayout))]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_utils", "ZInput", "m_buttons", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Collections.Generic.Dictionary\u00602[[System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[ZInput\u002BButtonDef, assembly_utils, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]]")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "Player", "TakeInput", Parameters = new string[0], Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Method, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Boolean")]
    internal static class SlotHotkeys
    {
        internal const int Count = 8; // 0-3 quick-use, 4-7 action

        private static readonly ConfigEntry<KeyboardShortcut>[] Entries = new ConfigEntry<KeyboardShortcut>[Count];
        private static readonly bool[] Claimed = new bool[Count];
        private static readonly HashSet<string> Suppressed = new HashSet<string>();
        private static readonly HashSet<Key> ClaimedKeys = new HashSet<Key>();
        private static readonly HashSet<Key> ShownKeys = new HashSet<Key>();
        private static int _frame = -1;
        private static bool _any;
        private static AccessTools_ButtonsRef _buttons;
        private static readonly Key[] Keys = new Key[Count];
        private static readonly EventHandler[] Changed = new EventHandler[Count];
        private static Func<Player, bool> _takeInput;
        private static bool _checking;
        private const string Owner = "module:hud.slots";

        /// <summary>On while the slots module runs: off, the input patch does nothing.</summary>
        internal static bool Active;

        internal static void Bind(ConfigFile config)
        {
            string[] defaults = { "Alpha1", "Alpha2", "Alpha3", "Alpha4", "Z", "X", "C", "V" };
            for (int i = 0; i < Count; i++)
            {
                bool quick = i < 4;
                int n = (quick ? i : i - 4) + 1;
                var key = (KeyCode)Enum.Parse(typeof(KeyCode), defaults[i]);
                Entries[i] = config.Bind("Hotkeys", (quick ? "Quick" : "Action") + n, new KeyboardShortcut(key, KeyCode.LeftAlt),
                    (quick ? "Atalho do espaço de consumo rápido " : "Atalho do espaço de ação ") + n +
                    ". Qualquer tecla, com ou sem Alt/Ctrl/Shift. Enquanto o atalho estiver apertado, a ação do jogo na mesma tecla não acontece (Alt+X não faz sentar).");
                int index = i;
                Changed[i] = (_, __) => { ToKey(Shortcut(index).MainKey, out Keys[index]); ResetClaims(); };
                Entries[i].SettingChanged += Changed[i];
                ToKey(Shortcut(i).MainKey, out Keys[i]);
            }
        }

        internal static void Resolve()
        {
            _takeInput = HarmonyLib.AccessTools.MethodDelegate<Func<Player, bool>>(HarmonyLib.AccessTools.Method(typeof(Player), "TakeInput"));
            _buttons = new AccessTools_ButtonsRef();
            ResetClaims();
        }

        internal static bool TakesInput(Player player)
        {
            if (!Active || _checking || player == null || _takeInput == null || InventoryModule.Current == null || Guard.IsTripped(Owner)) return false;
            _checking = true;
            try { return InputLeases.ActiveCount == 0 && !player.IsDead() && !player.InCutscene() && !player.IsTeleporting() && !Patches.TextInputFocus.Active && _takeInput(player); }
            finally { _checking = false; }
        }

        internal static void ResetClaims()
        {
            Array.Clear(Claimed, 0, Count);
            ClaimedKeys.Clear(); ShownKeys.Clear(); Suppressed.Clear(); _any = false; _frame = -1;
        }

        internal static void Shutdown()
        {
            Active = false; ResetClaims(); _takeInput = null; _buttons = null;
            for (int i = 0; i < Count; i++)
            {
                if (Entries[i] != null && Changed[i] != null) Entries[i].SettingChanged -= Changed[i];
                Entries[i] = null; Changed[i] = null;
            }
        }

        internal static KeyboardShortcut Shortcut(int index) => Entries[index] != null ? Entries[index].Value : KeyboardShortcut.Empty;

        /// <summary>Pressed this frame with exactly its modifiers.</summary>
        internal static bool Down(int index) => Entries[index] != null && Entries[index].Value.MainKey != KeyCode.None && Entries[index].Value.IsDown();

        /// <summary>
        /// Called by the input patch for every vanilla button read: true when that button's key belongs to
        /// a GenesisUI hotkey being held right now (vanilla then reads it as not pressed).
        /// </summary>
        internal static bool Suppresses(string button)
        {
            if (!Active || _checking || Guard.IsTripped(Owner)) return false;
            if (_frame != Time.frameCount && !Guard.Run(Owner, Update)) { ResetClaims(); return false; }
            return _any && button != null && Suppressed.Contains(button);
        }

        /// <summary>
        /// Once per frame: which hotkeys are held. A hotkey claims its key from the press of its full
        /// combination until the key is released (letting go of Alt first does not hand the key back).
        /// </summary>
        private static void Update()
        {
            _frame = Time.frameCount;
            if (!TakesInput(Player.m_localPlayer)) { ResetClaims(); _frame = Time.frameCount; return; }
            var layout = InventoryModule.Current;
            var input = BepInEx.UnityInput.Current;
            ClaimedKeys.Clear();
            for (int i = 0; i < Count; i++)
            {
                var s = Entries[i] != null ? Entries[i].Value : KeyboardShortcut.Empty;
                if (i < 4 ? i >= layout.Quick : i - 4 >= layout.Utility) { Claimed[i] = false; continue; }
                if (s.MainKey == KeyCode.None) { Claimed[i] = false; continue; }
                if (!Claimed[i] && s.IsPressed()) Claimed[i] = true;
                else if (Claimed[i] && !input.GetKey(s.MainKey)) Claimed[i] = false;
                if (Claimed[i] && Keys[i] != Key.None) ClaimedKeys.Add(Keys[i]);
            }
            _any = ClaimedKeys.Count > 0;
            // Read the live binding controls while a key is claimed: rebinding must take effect
            // even when the same key remains held. This enumeration allocates no strings.
            ShownKeys.Clear();
            ShownKeys.UnionWith(ClaimedKeys);
            Suppressed.Clear();
            if (_any) FindButtons();
        }

        private static void FindButtons()
        {
            if (ZInput.instance == null) return;
            if (_buttons == null) return;
            var buttons = _buttons.Get(ZInput.instance);
            if (buttons == null) return;
            foreach (var pair in buttons)
            {
                var action = pair.Value != null ? pair.Value.ButtonAction : null;
                if (action == null) continue;
                foreach (var control in action.controls)
                    if (control is KeyControl k && ShownKeys.Contains(k.keyCode)) { Suppressed.Add(pair.Key); break; }
            }
        }

        /// <summary>The Input System key of a legacy key code (by name, with the few names that differ).</summary>
        internal static bool ToKey(KeyCode code, out Key key)
        {
            key = Key.None;
            if (code >= KeyCode.Alpha0 && code <= KeyCode.Alpha9) { key = code == KeyCode.Alpha0 ? Key.Digit0 : Key.Digit1 + (code - KeyCode.Alpha1); return true; }
            if (code >= KeyCode.Keypad0 && code <= KeyCode.Keypad9) { key = Key.Numpad0 + (code - KeyCode.Keypad0); return true; }
            switch (code)
            {
                case KeyCode.Return: key = Key.Enter; return true;
                case KeyCode.LeftControl: key = Key.LeftCtrl; return true;
                case KeyCode.RightControl: key = Key.RightCtrl; return true;
                case KeyCode.BackQuote: key = Key.Backquote; return true;
                case KeyCode.KeypadEnter: key = Key.NumpadEnter; return true;
                case KeyCode.LeftCommand: key = Key.LeftCommand; return true;
                case KeyCode.RightCommand: key = Key.RightCommand; return true;
            }
            return Enum.TryParse(code.ToString(), out key) && key != Key.None;
        }

        /// <summary>ZInput's private table of buttons, read through reflection once.</summary>
        private sealed class AccessTools_ButtonsRef
        {
            private readonly HarmonyLib.AccessTools.FieldRef<ZInput, Dictionary<string, ZInput.ButtonDef>> _ref =
                HarmonyLib.AccessTools.FieldRefAccess<ZInput, Dictionary<string, ZInput.ButtonDef>>("m_buttons");

            internal Dictionary<string, ZInput.ButtonDef> Get(ZInput input) => _ref(input);
        }
    }
}
