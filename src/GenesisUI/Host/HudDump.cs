using System.Collections.Generic;
using System.Text;
using GenesisUI.Foundation;
using GenesisUI.Foundation.Contracts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GenesisUI.Host
{
    /// <summary>
    /// Text snapshot of a vanilla UI subtree for the diagnostic report: what exists, what is
    /// active, what it draws, and whether GenesisUI veils it. Lets a test report answer
    /// "what is that leftover under our bars?" without guessing.
    /// </summary>
    [GameContract("assembly_valheim", "Hud", "m_healthPanel")]
    internal static class HudDump
    {
        private const int MaxLines = 120;

        public static IEnumerable<string> HealthPanel()
        {
            var hud = Hud.instance;
            if (hud == null || hud.m_healthPanel == null) return new[] { "(no HUD: open the report in a world)" };
            var lines = new List<string>();
            Walk(hud.m_healthPanel, 0, 4, lines);
            return lines;
        }

        private static void Walk(Transform t, int depth, int maxDepth, List<string> lines)
        {
            if (lines.Count >= MaxLines) return;
            var sb = new StringBuilder();
            sb.Append(' ', depth * 2).Append(t.name);
            if (!t.gameObject.activeSelf) sb.Append(" [inactive]");
            if (IsVeiled(t)) sb.Append(" [veiled]");
            var img = t.GetComponent<Image>();
            if (img != null) sb.Append(" image:").Append(img.sprite != null ? img.sprite.name : "none").Append(img.enabled ? "" : "(off)");
            var text = t.GetComponent<TMP_Text>();
            if (text != null) sb.Append(" text:\"").Append(text.text.Length > 20 ? text.text.Substring(0, 20) : text.text).Append('"');
            lines.Add(sb.ToString());
            if (depth >= maxDepth) return;
            foreach (Transform child in t) Walk(child, depth + 1, maxDepth, lines);
        }

        private static bool IsVeiled(Transform t)
        {
            foreach (var h in VanillaVeil.Handles)
                if (h.Target != null && (h.Target.transform == t || t.IsChildOf(h.Target.transform))) return true;
            return false;
        }
    }
}
