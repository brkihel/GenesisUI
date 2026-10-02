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
            if (!Active) return false;
            if (_frame != Time.frameCount) Update();
            return _any && button != null && Suppressed.Contains(button);
        }

        /// <summary>
        /// Once per frame: which hotkeys are held. A hotkey claims its key from the press of its full
        /// combination until the key is released (letting go of Alt first does not hand the key back).
        /// </summary>
        private static void Update()
        {
            _frame = Time.frameCount;
            var input = BepInEx.UnityInput.Current;
            ClaimedKeys.Clear();
            for (int i = 0; i < Count; i++)
            {
                var s = Entries[i] != null ? Entries[i].Value : KeyboardShortcut.Empty;
                if (s.MainKey == KeyCode.None) { Claimed[i] = false; continue; }
                if (!Claimed[i] && s.IsPressed()) Claimed[i] = true;
                else if (Claimed[i] && !input.GetKey(s.MainKey)) Claimed[i] = false;
                if (Claimed[i] && ToKey(s.MainKey, out var key)) ClaimedKeys.Add(key);
            }
            _any = ClaimedKeys.Count > 0;
            if (ClaimedKeys.SetEquals(ShownKeys)) return;
            // The held keys changed (rare): find vanilla's buttons bound to them.
            ShownKeys.Clear();
            ShownKeys.UnionWith(ClaimedKeys);
            Suppressed.Clear();
            if (_any) Guard.Try("hotkeys: vanilla buttons", FindButtons);
        }

        private static void FindButtons()
        {
            if (ZInput.instance == null) return;
            if (_buttons == null) _buttons = new AccessTools_ButtonsRef();
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
