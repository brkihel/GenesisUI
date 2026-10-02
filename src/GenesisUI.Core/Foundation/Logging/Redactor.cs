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
        private static readonly Regex Ipv6 = new Regex(@"(?<![\w:])(?:[0-9a-f]{1,4}(?::[0-9a-f]{1,4}){0,6})?::(?:[0-9a-f]{1,4}(?::[0-9a-f]{1,4}){0,6})?(?![\w:])|(?<![\w:])(?:[0-9a-f]{1,4}:){7}[0-9a-f]{1,4}(?![\w:])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        private static readonly Regex Host = new Regex(@"(?im)\b(server|host|endpoint|address)(\s*[:=]\s*|\s+)(?:https?://)?[a-z0-9][a-z0-9.-]*\.[a-z]{2,63}(?::\d{1,5})?", RegexOptions.CultureInvariant);
        private static readonly Regex UserPath = new Regex(@"(?i)([a-z]:[\\/]Users[\\/])[^\\/\r\n]+|(/home/)[^/\r\n]+", RegexOptions.CultureInvariant);

        private readonly object _lock = new object();
        private readonly List<KeyValuePair<string, string>> _known = new List<KeyValuePair<string, string>>();

        /// <summary>Explicit short context values use whole-word matches; other values below 3 characters are ignored.</summary>
        public void AddSecret(string value, string label, bool includeShort = false)
        {
            if (string.IsNullOrWhiteSpace(value) || (!includeShort && value.Trim().Length < 3)) return;
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
                text = k.Key.Length < 3
                    ? Regex.Replace(text, @"(?<![\w.])" + Regex.Escape(k.Key) + @"(?![\w.])", _ => "<" + k.Value + ">", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)
                    : ReplaceOrdinalIgnoreCase(text, k.Key, "<" + k.Value + ">");

            text = SteamId.Replace(text, "<platform-id>");
            text = Ipv4.Replace(text, "<address>");
            text = Ipv6.Replace(text, "<address>");
            text = Host.Replace(text, "$1$2<address>");
            text = UserPath.Replace(text, "<user-path>");
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
