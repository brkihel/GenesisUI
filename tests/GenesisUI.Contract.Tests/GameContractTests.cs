using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;

namespace GenesisUI.Contract.Tests
{
    /// <summary>
    /// Every [GameContract] declared in GenesisUI.dll must resolve against the game
    /// assemblies in ref/. When Iron Gate ships an update, this names the broken
    /// member before a player loads the game (docs/PATCH-POLICY.md rule 6).
    /// </summary>
    public class GameContractTests
    {
        /// <summary>The game build the contracts were last verified against (ref/SOURCE.txt at that time).</summary>
        public const string VerifiedAgainst = "Valheim l-1.0.16, build 25527701";

        private const string ContractAttribute = "GenesisUI.Foundation.Contracts.GameContractAttribute";
        private const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        private static MetadataLoadContext Load(out Assembly plugin)
        {
            var paths = Directory.GetFiles(TestPaths.RefDir, "*.dll").ToList();
            var names = new HashSet<string>(paths.Select(Path.GetFileNameWithoutExtension), StringComparer.OrdinalIgnoreCase);
            var runtime = Path.GetDirectoryName(typeof(object).Assembly.Location);
            paths.AddRange(Directory.GetFiles(runtime, "*.dll").Where(p => !names.Contains(Path.GetFileNameWithoutExtension(p))));
            paths.Add(TestPaths.PluginDll);

            var ctx = new MetadataLoadContext(new PathAssemblyResolver(paths), "mscorlib");
            plugin = ctx.LoadFromAssemblyPath(TestPaths.PluginDll);
            return ctx;
        }

        private static IEnumerable<(Type owner, string asm, string type, string member, string[] parameters)> Contracts(Assembly plugin)
        {
            foreach (var t in plugin.GetTypes())
            {
                foreach (var a in t.GetCustomAttributesData().Where(a => a.AttributeType.FullName == ContractAttribute))
                {
                    string[] parameters = null;
                    foreach (var named in a.NamedArguments)
                    {
                        if (named.MemberName == "Parameters" && named.TypedValue.Value is IReadOnlyCollection<CustomAttributeTypedArgument> items)
                            parameters = items.Select(i => (string)i.Value).ToArray();
                    }

                    yield return (t, (string)a.ConstructorArguments[0].Value, (string)a.ConstructorArguments[1].Value,
                                  (string)a.ConstructorArguments[2].Value, parameters);
                }
            }
        }

        [SkippableFact]
        public void Every_declared_game_member_exists()
        {
            TestPaths.SkipUnlessRefs();
            TestPaths.SkipUnlessPlugin();

            using var ctx = Load(out var plugin);
            var contracts = Contracts(plugin).ToList();
            Assert.NotEmpty(contracts);

            var missing = new List<string>();
            foreach (var c in contracts)
            {
                string where = c.owner.FullName + " -> " + c.asm + ":" + c.type + "::" + c.member;
                string file = Path.Combine(TestPaths.RefDir, c.asm + ".dll");
                if (!File.Exists(file)) { missing.Add(where + " (assembly not in ref/)"); continue; }

                var type = ctx.LoadFromAssemblyPath(file).GetType(c.type, false);
                if (type == null) { missing.Add(where + " (type not found)"); continue; }

                var members = type.GetMember(c.member, All);
                if (members.Length == 0) { missing.Add(where + " (member not found)"); continue; }

                if (c.parameters != null && !members.OfType<MethodBase>().Any(m =>
                        m.GetParameters().Select(p => p.ParameterType.FullName).SequenceEqual(c.parameters)))
                    missing.Add(where + " (no overload (" + string.Join(", ", c.parameters) + "))");
            }

            Assert.True(missing.Count == 0,
                "Game contracts broken (verified against " + VerifiedAgainst + "):\n" + string.Join("\n", missing));
        }

        [SkippableFact]
        public void Every_harmony_patch_class_declares_its_contracts()
        {
            TestPaths.SkipUnlessRefs();
            TestPaths.SkipUnlessPlugin();

            using var ctx = Load(out var plugin);
            var withContracts = new HashSet<Type>(Contracts(plugin).Select(c => c.owner));
            var offenders = plugin.GetTypes()
                .Where(t => t.GetCustomAttributesData().Any(a => a.AttributeType.FullName == "HarmonyLib.HarmonyPatch"))
                .Where(t => !withContracts.Contains(t))
                // ServerSync is merged third-party code with its own self-contained patches (D-031).
                .Where(t => !(t.FullName ?? "").StartsWith("ServerSync.", StringComparison.Ordinal))
                .Select(t => t.FullName)
                .ToList();

            Assert.True(offenders.Count == 0, "Patch classes without [GameContract] (PATCH-POLICY rule 6):\n" + string.Join("\n", offenders));
        }

        [SkippableFact]
        public void Core_is_merged_into_the_plugin()
        {
            TestPaths.SkipUnlessRefs();
            TestPaths.SkipUnlessPlugin();

            using var ctx = Load(out var plugin);
            // Shipped beside the plugin, the Core is never loaded by BepInEx (see ILRepack.targets).
            Assert.NotNull(plugin.GetType("GenesisUI.Foundation.Faults.FaultRegistry", false));
            Assert.NotNull(plugin.GetType(ContractAttribute, false));
            Assert.DoesNotContain(plugin.GetReferencedAssemblies(), a => a.Name == "GenesisUI.Core");
        }

        [SkippableFact]
        public void Plugin_does_not_enforce_network_compatibility()
        {
            TestPaths.SkipUnlessRefs();
            TestPaths.SkipUnlessPlugin();

            using var ctx = Load(out var plugin);
            var attr = plugin.GetType("GenesisUI.Plugin").GetCustomAttributesData()
                .Single(a => a.AttributeType.FullName == "Jotunn.Utils.NetworkCompatibilityAttribute");

            // D-001: players with and without GenesisUI must be able to share a server.
            Assert.Equal("NotEnforced", LevelName(attr));
        }

        private static string LevelName(CustomAttributeData attr)
        {
            var enumType = attr.ConstructorArguments[0].ArgumentType;
            int value = Convert.ToInt32(attr.ConstructorArguments[0].Value);
            return enumType.GetFields(BindingFlags.Public | BindingFlags.Static)
                .First(f => Convert.ToInt32(f.GetRawConstantValue()) == value).Name;
        }
    }
}
