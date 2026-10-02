using System.Text;
using BepInEx.Configuration;
using UnityEngine;

namespace GenesisUI.Widgets
{
    /// <summary>How a key or a combination is written for players: "Alt+Z", "Ctrl+1", "F2", "Esc".</summary>
    internal static class KeyText
    {
        public static string Of(KeyboardShortcut shortcut)
        {
            if (shortcut.MainKey == KeyCode.None) return "—";
            var sb = new StringBuilder();
            foreach (var m in shortcut.Modifiers) sb.Append(Key(m)).Append('+');
            sb.Append(Key(shortcut.MainKey));
            return sb.ToString();
        }

        /// <summary>The same, compact for a small cell ("A+Z" would be cryptic: modifiers keep their name, without spaces).</summary>
        public static string Short(KeyboardShortcut shortcut) => shortcut.MainKey == KeyCode.None ? "" : Of(shortcut);

        public static string Key(KeyCode key)
        {
            switch (key)
            {
                case KeyCode.None: return "—";
                case KeyCode.Escape: return "Esc";
                case KeyCode.LeftShift: case KeyCode.RightShift: return "Shift";
                case KeyCode.LeftControl: case KeyCode.RightControl: return "Ctrl";
                case KeyCode.LeftAlt: case KeyCode.RightAlt: return "Alt";
                case KeyCode.Return: return "Enter";
                case KeyCode.BackQuote: return "'";
                case KeyCode.Space: return "Space";
                default:
                    string s = key.ToString();
                    if (s.StartsWith("Alpha")) return s.Substring(5);
                    if (s.StartsWith("Keypad")) return "Num" + s.Substring(6);
                    return s;
            }
        }

        /// <summary>True for keys that only modify another key.</summary>
        public static bool IsModifier(KeyCode key) =>
            key == KeyCode.LeftAlt || key == KeyCode.RightAlt || key == KeyCode.LeftControl || key == KeyCode.RightControl ||
            key == KeyCode.LeftShift || key == KeyCode.RightShift || key == KeyCode.LeftCommand || key == KeyCode.RightCommand || key == KeyCode.AltGr;
    }
}
