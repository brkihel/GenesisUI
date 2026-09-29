using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using GenesisUI.Foundation.Contracts;
using GenesisUI.Foundation.Logging;
using HarmonyLib;
using UnityEngine;

namespace GenesisUI.Foundation
{
    /// <summary>
    /// Writes the diagnostic report (docs/DIAGNOSTICS.md §4) to
    /// BepInEx/&lt;Product&gt;/reports/ and copies its path to the clipboard.
    /// Personal data is redacted unless the player turned redaction off.
    /// </summary>
    [GameContract("assembly_valheim", "Game", "GetPlayerProfile")]
    [GameContract("assembly_valheim", "PlayerProfile", "GetName")]
    [GameContract("assembly_valheim", "ZNet", "GetWorldName")]
    [GameContract("assembly_valheim", "ZNet", "m_serverHost")]
    internal static class ReportWriter
    {
        /// <param name="sections">Extra titled sections from the caller (modules, overlay panels...).</param>
        /// <returns>The full path of the written file, or null if it could not be written.</returns>
        public static string Write(string product, IReadOnlyList<string> header, ConfigFile config, bool redact,
                                   IEnumerable<KeyValuePair<string, IEnumerable<string>>> sections = null)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("=== " + product + " diagnostic report — " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture) + " ===");
                sb.AppendLine(redact ? "(personal data redacted)" : "(REDACTION OFF — may contain personal data)");
                Section(sb, "Session", header);

                Section(sb, "Faults", Guard.Faults.Snapshot().Select(f =>
                    f.Owner + (f.Tripped ? " [TRIPPED]" : "") + " x" + f.Count + ": " + f.FirstMessage));
                Section(sb, "Patches", GuardedPatcher.Results.Select(r =>
                    r.State + " " + r.PatchClass + (r.Reason == null ? "" : " — " + r.Reason)));
                Section(sb, "Input leases", new[] { "active: " + InputLeases.ActiveCount });

                if (sections != null)
                    foreach (var s in sections) Section(sb, s.Key, s.Value);

                if (config != null)
                    Section(sb, "Config", config.Keys.OrderBy(k => k.Section).ThenBy(k => k.Key)
                        .Select(k => "[" + k.Section + "] " + k.Key + " = " + config[k].BoxedValue));

                Section(sb, "Recent log", GenesisLog.Recent());

                string text = sb.ToString();
                if (redact) text = BuildRedactor().Redact(text);

                string directory = Path.GetFullPath(Path.Combine(Paths.BepInExRootPath, product, "reports"));
                Directory.CreateDirectory(directory);
                string path = Path.GetFullPath(Path.Combine(directory, "report-" + DateTime.Now.ToString("yyyyMMdd-HHmmss", System.Globalization.CultureInfo.InvariantCulture) + ".log"));
                // The name is ours, but check anyway: reports never land outside their folder.
                if (!path.StartsWith(directory + Path.DirectorySeparatorChar, StringComparison.Ordinal)) return null;

                File.WriteAllText(path, text, new UTF8Encoding(false));
                try { GUIUtility.systemCopyBuffer = path; } catch { }
                GenesisLog.Info("Report", "written to " + path);
                return path;
            }
            catch (Exception e)
            {
                GenesisLog.Error("Report", "could not write the report: " + e);
                return null;
            }
        }

        private static void Section(StringBuilder sb, string title, IEnumerable<string> lines)
        {
            sb.AppendLine().AppendLine("--- " + title + " ---");
            int n = 0;
            foreach (var l in lines)
            {
                sb.AppendLine(l);
                n++;
            }
            if (n == 0) sb.AppendLine("(none)");
        }

        private static Redactor BuildRedactor()
        {
            var r = new Redactor();
            Try(() => r.AddSecret(Game.instance?.GetPlayerProfile()?.GetName(), "character"));
            Try(() => r.AddSecret(ZNet.instance?.GetWorldName(), "world"));
            Try(() => r.AddSecret(AccessTools.Field(typeof(ZNet), "m_serverHost")?.GetValue(null) as string, "server"));
            Try(() => r.AddSecret(Environment.UserName, "os-user"));
            return r;
        }

        private static void Try(Action a)
        {
            try { a(); } catch { }
        }
    }
}
