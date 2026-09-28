using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace GenesisUI.Foundation.Logging
{
    /// <summary>
    /// Removes personal data from diagnostic reports before they are written
    /// (docs/DIAGNOSTICS.md §4). Known values (player name, world name, server
    /// address) are registered at runtime; Steam IDs and IP addresses are caught by
    /// pattern.
    /// </summary>
    public sealed class Redactor
    {
        // 17-digit SteamID64 (individual accounts start with 7656119).
        private static readonly Regex SteamId = new Regex(@"\b7656119\d{10}\b", RegexOptions.CultureInvariant);

        // IPv4 with a port, or IPv4 whose first octet has 2-3 digits. Plain dotted
        // versions such as "1.0.15.0" or "5.4.2333.0" must survive: they are the
        // most useful lines of a report.
        private static readonly Regex Ipv4 = new Regex(
            @"\b(?:(?:25[0-5]|2[0-4]\d|1?\d?\d)\.){3}(?:25[0-5]|2[0-4]\d|1?\d?\d):\d{1,5}\b|\b(?:25[0-5]|2[0-4]\d|1\d\d|[1-9]\d)\.(?:(?:25[0-5]|2[0-4]\d|1?\d?\d)\.){2}(?:25[0-5]|2[0-4]\d|1?\d?\d)\b",
            RegexOptions.CultureInvariant);

        private readonly object _lock = new object();
        private readonly List<KeyValuePair<string, string>> _known = new List<KeyValuePair<string, string>>();

        /// <summary>Registers a value to hide. Values shorter than 3 characters are ignored: they would erase ordinary words.</summary>
        public void AddSecret(string value, string label)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Trim().Length < 3) return;
            lock (_lock)
            {
                if (_known.Any(k => k.Key == value)) return;
                _known.Add(new KeyValuePair<string, string>(value, label));
                // Longest first, so "Eirik Stormborn" is replaced before "Eirik".
                _known.Sort((a, b) => b.Key.Length.CompareTo(a.Key.Length));
            }
        }

        public string Redact(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            List<KeyValuePair<string, string>> known;
            lock (_lock) known = _known.ToList();

            foreach (var k in known)
                text = ReplaceOrdinalIgnoreCase(text, k.Key, "<" + k.Value + ">");

            text = SteamId.Replace(text, "<platform-id>");
            text = Ipv4.Replace(text, "<address>");
            return text;
        }

        private static string ReplaceOrdinalIgnoreCase(string text, string find, string replacement)
        {
            int i = text.IndexOf(find, StringComparison.OrdinalIgnoreCase);
            if (i < 0) return text;
            var sb = new System.Text.StringBuilder(text.Length);
            int start = 0;
            while (i >= 0)
            {
                sb.Append(text, start, i - start).Append(replacement);
                start = i + find.Length;
                i = text.IndexOf(find, start, StringComparison.OrdinalIgnoreCase);
            }
            return sb.Append(text, start, text.Length - start).ToString();
        }
    }
}
