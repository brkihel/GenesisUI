using System;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace GenesisUI.Contract.Tests
{
    /// <summary>
    /// Foundation is the future GenesisModLIB (docs/DECISIONS.md D-015): it may use
    /// the game, Unity, BepInEx, Harmony and Jötunn, but never anything of GenesisUI
    /// outside Foundation. If this passes, extraction is a move, not a rewrite.
    /// </summary>
    public class FoundationIsolationTests
    {
        private static readonly Regex GenesisType = new Regex(@"GenesisUI(\.[A-Za-z_][\w`]*)+", RegexOptions.CultureInvariant);

        private static bool IsFoundation(string typeName) =>
            typeName.StartsWith("GenesisUI.Foundation.", StringComparison.Ordinal) ||
            typeName.StartsWith("GenesisUI.Foundation/", StringComparison.Ordinal);

        [SkippableFact]
        public void Foundation_references_nothing_outside_foundation()
        {
            TestPaths.SkipUnlessPlugin();

            var violations = IlScanner.Scan(TestPaths.PluginDll)
                .Where(r => IsFoundation(r.CallerType))
                .SelectMany(r => GenesisType.Matches(r.Target).Select(m => (r, used: m.Value)))
                .Where(x => !x.used.StartsWith("GenesisUI.Foundation.", StringComparison.Ordinal))
                .Select(x => x.r.CallerType + "::" + x.r.CallerMethod + " uses " + x.used)
                .Distinct()
                .ToList();

            Assert.True(violations.Count == 0, "Foundation depends on GenesisUI code:\n" + string.Join("\n", violations));
        }

        [SkippableFact]
        public void Foundation_types_exist()
        {
            TestPaths.SkipUnlessPlugin();

            var callers = IlScanner.Scan(TestPaths.PluginDll).Select(r => r.CallerType).Where(IsFoundation).ToHashSet();

            Assert.Contains("GenesisUI.Foundation.Guard", callers);
            Assert.Contains("GenesisUI.Foundation.GuardedPatcher", callers);
            Assert.Contains("GenesisUI.Foundation.Faults.FaultRegistry", callers);
        }
    }
}
