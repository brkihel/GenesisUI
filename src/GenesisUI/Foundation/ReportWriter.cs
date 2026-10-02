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
    [GameContract("assembly_valheim", "ZNet", "m_serverHost", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Static, ValueType = "System.String")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "Game", "get_instance", Parameters = new string[] {  }, Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Method, Static = GenesisUI.Foundation.Contracts.ContractStatic.Static, ValueType = "Game")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "ZNet", "get_instance", Parameters = new string[] {  }, Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Method, Static = GenesisUI.Foundation.Contracts.ContractStatic.Static, ValueType = "ZNet")]
    [GenesisUI.Foundation.Contracts.ContractDependency(typeof(GenesisUI.Foundation.Guard), typeof(GenesisUI.Foundation.Faults.FaultRegistry), typeof(GenesisUI.Foundation.GuardedPatcher), typeof(GenesisUI.Foundation.InputLeases), typeof(GenesisUI.Foundation.GenesisLog), typeof(GenesisUI.Foundation.Logging.Redactor), typeof(GenesisUI.Foundation.Faults.FaultRecord), typeof(GenesisUI.Foundation.PatchResult))]
    internal static class ReportWriter
    {
        private const int MaxCharacters = 1024 * 1024, MaxLine = 16384, KeepReports = 10;
        private static readonly System.Text.RegularExpressions.Regex OwnedName = new System.Text.RegularExpressions.Regex(@"^report-\d{8}-\d{6}(?:-\d{3}-[a-f0-9]{32})?\.log$", System.Text.RegularExpressions.RegexOptions.CultureInvariant);
        /// <param name="sections">Extra titled sections from the caller (modules, overlay panels...).</param>
        /// <returns>The full path of the written file, or null if it could not be written.</returns>
        public static string Write(string product, IReadOnlyList<string> header, ConfigFile config, bool redact,
                                   IEnumerable<KeyValuePair<string, IEnumerable<string>>> sections = null)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("=== " + product + " diagnostic report — " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture) + " ===");
                sb.AppendLine("schema: 2; maximum report characters: " + MaxCharacters + "; retention: " + KeepReports);
                sb.AppendLine(redact ? "(personal data redacted; context collection must succeed)" : "(REDACTION OFF — may contain personal data)");
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
                        .Select(k => "[" + k.Section + "] " + k.Key + " = " + (SensitiveKey(k.Key) && redact ? "<secret>" : config[k].BoxedValue)));

                Section(sb, "Recent log", GenesisLog.Recent());

                string text = sb.ToString();
                if (redact) text = BuildRedactor().Redact(text);
                if (text.Length > MaxCharacters) text = text.Substring(0, MaxCharacters - 64) + "\n[redacted report truncated at size limit]\n";

                string directory = Path.GetFullPath(Path.Combine(Paths.BepInExRootPath, product, "reports"));
                Directory.CreateDirectory(directory);
                string path = Path.GetFullPath(Path.Combine(directory, "report-" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff", System.Globalization.CultureInfo.InvariantCulture) + "-" + Guid.NewGuid().ToString("N") + ".log"));
                // The name is ours, but check anyway: reports never land outside their folder.
                if (!path.StartsWith(directory + Path.DirectorySeparatorChar, StringComparison.Ordinal)) return null;

                File.WriteAllText(path, text, new UTF8Encoding(false));
                Guard.Try("report retention", () =>
                {
                    var files = new DirectoryInfo(directory).GetFiles("report-*.log").Where(f => OwnedName.IsMatch(f.Name)).OrderByDescending(f => f.LastWriteTimeUtc).ThenByDescending(f => f.Name).Skip(KeepReports);
                    foreach (var old in files) if (old.FullName.StartsWith(directory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) old.Delete();
                });
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
            if (sb.Length >= MaxCharacters - 128) return;
            sb.AppendLine().AppendLine("--- " + title + " ---");
            int n = 0;
            foreach (var l in lines)
            {
                int remaining = MaxCharacters - sb.Length - 128;
                if (remaining <= 0) { sb.AppendLine("[report truncated at size limit]"); break; }
                string line = l ?? "";
                int take = Math.Min(line.Length, Math.Min(MaxLine, remaining));
                sb.Append(line, 0, take).AppendLine(take < line.Length ? " [line truncated]" : "");
                n++;
            }
            if (n == 0) sb.AppendLine("(none)");
        }

        private static Redactor BuildRedactor()
        {
            var r = new Redactor();
            // A collection error fails the report closed; never silently claim successful redaction.
            if (Game.instance != null) r.AddSecret(Game.instance.GetPlayerProfile()?.GetName(), "character", includeShort: true);
            if (ZNet.instance != null) r.AddSecret(ZNet.instance.GetWorldName(), "world", includeShort: true);
            var host = AccessTools.Field(typeof(ZNet), "m_serverHost");
            if (host == null) throw new InvalidOperationException("Redaction context unavailable: ZNet.m_serverHost");
            r.AddSecret(host.GetValue(null) as string, "server");
            r.AddSecret(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "user-path");
            r.AddSecret(Environment.UserName, "os-user", includeShort: true);
            return r;
        }
        private static bool SensitiveKey(string key) => key.IndexOf("password", StringComparison.OrdinalIgnoreCase) >= 0 || key.IndexOf("token", StringComparison.OrdinalIgnoreCase) >= 0 || key.IndexOf("secret", StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
