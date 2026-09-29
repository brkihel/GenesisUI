using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace GenesisUI.Contract.Tests
{
    /// <summary>
    /// Enforces the hard rules of AGENTS.md §2 on the compiled plugin: the UI never
    /// changes game state, never talks to the network, never removes other mods'
    /// patches, never patches the whole assembly, and writes files only where
    /// diagnostics are allowed to. An exception needs a decision entry and a line in
    /// <see cref="Allowed"/> naming the calling type.
    /// </summary>
    public class BannedApiTests
    {
        private static readonly (string pattern, string why)[] Banned =
        {
            (@"^ZNetView::(ClaimOwnership|Register|InvokeRPC|Destroy)$", "UI must not own or mutate networked objects (D-001, D-008)"),
            (@"^ZRoutedRpc::", "no RPC in the visual scope (D-001)"),
            (@"^ZDO::(Set|Release|Reset)", "UI must not write ZDOs (D-008)"),
            (@"^ZDOMan::", "UI must not touch the ZDO manager (D-008)"),
            (@"^Inventory::(AddItem|RemoveItem|RemoveOneItem|RemoveAll|MoveItemToThis|MoveAll|MoveInventoryToGrave)$", "item changes only through vanilla UI entry points (D-008)"),
            (@"^HarmonyLib\.Harmony::(PatchAll|PatchAllUncategorized|Unpatch|UnpatchAll|UnpatchSelf|UnpatchID|UnpatchCategory)$", "per-class patching only; never unpatch others (D-006)"),
            (@"^HarmonyLib\.Harmony::CreateAndPatchAll$", "per-class patching only (D-006)"),
            (@"^System\.IO\.File::(WriteAll|AppendAll|AppendText|Create|CreateText|Delete|Move|Copy|Replace|Open|OpenWrite|SetAttributes)", "file writes only in diagnostics writers"),
            (@"^System\.IO\.(FileInfo::(Delete|MoveTo|CopyTo|Create|CreateText|AppendText|Open|OpenWrite|Replace)|Directory::(Delete|Move))$", "file writes only in diagnostics writers"),
            (@"^System\.IO\.(FileStream|StreamWriter|BinaryWriter)::\.ctor$", "file writes only in diagnostics writers"),
            (@"^System\.Net\.", "no network access"),
            (@"^UnityEngine\.Networking\.", "no network access"),
            (@"^System\.Diagnostics\.Process::Start$", "no process launching"),
            (@"^System\.Reflection\.Assembly::(Load|LoadFrom|LoadFile|UnsafeLoadFrom)$", "no loading code at runtime"),
        };

        /// <summary>Caller type prefix, target pattern, reason. Keep this list short and justified.</summary>
        private static readonly (string caller, string pattern, string why)[] Allowed =
        {
            ("GenesisUI.Foundation.LogFileSink", @"^System\.IO\.(FileStream|StreamWriter)::\.ctor$|^System\.IO\.FileInfo::Delete$",
                "own log folder BepInEx/GenesisUI/logs, size-capped, newest 5 kept (DIAGNOSTICS.md §2)"),
            ("GenesisUI.Foundation.ReportWriter", @"^System\.IO\.File::WriteAllText$",
                "reports folder BepInEx/GenesisUI/reports, path checked (DIAGNOSTICS.md §4)"),
            ("ServerSync.", @"^ZRoutedRpc::|^HarmonyLib\.Harmony::PatchAll$|^System\.IO\.File::|^System\.IO\.FileStream::\.ctor$",
                "ServerSync (D-031): server-to-client config sync; patches only its own nested classes"),
            ("GenesisUI.Foundation.GuardedPatcher", @"^HarmonyLib\.Harmony::Unpatch$",
                "rollback of our own patch methods only, filtered by our Harmony id and patch class (D-016)"),
        };

        private static bool IsAllowed(Reference r) =>
            Allowed.Any(a => (r.CallerType == a.caller || r.CallerType.StartsWith(a.caller + "/", StringComparison.Ordinal)
                              // A caller ending in '.' names a whole merged namespace (ServerSync, D-031).
                              || (a.caller.EndsWith(".", StringComparison.Ordinal) && r.CallerType.StartsWith(a.caller, StringComparison.Ordinal)))
                             && Regex.IsMatch(r.Target, a.pattern));

        [SkippableFact]
        public void Plugin_calls_no_banned_api()
        {
            TestPaths.SkipUnlessPlugin();

            var violations = new List<string>();
            foreach (var r in IlScanner.Scan(TestPaths.PluginDll).Where(r => !r.Target.StartsWith("type:", StringComparison.Ordinal)))
            {
                var hit = Banned.FirstOrDefault(b => Regex.IsMatch(r.Target, b.pattern));
                if (hit.pattern != null && !IsAllowed(r)) violations.Add(r + "   [" + hit.why + "]");
            }

            Assert.True(violations.Count == 0, "Banned API calls:\n" + string.Join("\n", violations.Distinct()));
        }

        [SkippableFact]
        public void Scanner_sees_known_calls()
        {
            // Guards the guard: if the scanner silently stopped reading IL, the test above would pass forever.
            TestPaths.SkipUnlessPlugin();

            var targets = IlScanner.Scan(TestPaths.PluginDll).Select(r => r.Target).ToHashSet();

            Assert.Contains("System.IO.File::WriteAllText", targets);
            Assert.Contains("HarmonyLib.Harmony::Unpatch", targets);
            Assert.Contains("Jotunn.Managers.GUIManager::BlockInput", targets);
        }
    }
}
