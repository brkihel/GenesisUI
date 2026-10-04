using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;
using GenesisUI.Foundation.Contracts;

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

        internal static MetadataLoadContext Load(out Assembly plugin)
        {
            var paths = Directory.GetFiles(TestPaths.RefDir, "*.dll").ToList();
            string adapters = Path.Combine(TestPaths.RefDir, "adapters");
            if (Directory.Exists(adapters)) paths.AddRange(Directory.GetFiles(adapters, "*.dll"));
            var names = new HashSet<string>(paths.Select(Path.GetFileNameWithoutExtension), StringComparer.OrdinalIgnoreCase);
            var runtime = Path.GetDirectoryName(typeof(object).Assembly.Location);
            paths.AddRange(Directory.GetFiles(runtime, "*.dll").Where(p => !names.Contains(Path.GetFileNameWithoutExtension(p))));
            paths.Add(TestPaths.PluginDll);

            var ctx = new MetadataLoadContext(new PathAssemblyResolver(paths), "mscorlib");
            plugin = ctx.LoadFromAssemblyPath(TestPaths.PluginDll);
            return ctx;
        }

        internal static IEnumerable<(Type owner, string asm, string type, string member, string[] parameters, GameContractAttribute shape)> Contracts(Assembly plugin)
        {
            foreach (var t in plugin.GetTypes())
            {
                foreach (var a in t.GetCustomAttributesData().Where(a => a.AttributeType.FullName == ContractAttribute))
                {
                    string[] parameters = null;
                    var shape = new GameContractAttribute((string)a.ConstructorArguments[0].Value, (string)a.ConstructorArguments[1].Value, (string)a.ConstructorArguments[2].Value);
                    foreach (var named in a.NamedArguments)
                    {
                        if (named.MemberName == "Parameters" && named.TypedValue.Value is IReadOnlyCollection<CustomAttributeTypedArgument> items)
                            parameters = items.Select(i => (string)i.Value).ToArray();
                        if (named.MemberName == "ValueType") shape.ValueType = (string)named.TypedValue.Value;
                        if (named.MemberName == "Kind") shape.Kind = (ContractMemberKind)Convert.ToInt32(named.TypedValue.Value);
                        if (named.MemberName == "Static") shape.Static = (ContractStatic)Convert.ToInt32(named.TypedValue.Value);
                    }

                    shape.Parameters = parameters;
                    yield return (t, shape.Assembly, shape.Type, shape.Member, parameters, shape);
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
                if (IsAdapter(c.asm)) continue;
                string where = c.owner.FullName + " -> " + c.asm + ":" + c.type + "::" + c.member;
                string file = c.asm == "GenesisUI" ? TestPaths.PluginDll : Path.Combine(TestPaths.RefDir, c.asm + ".dll");
                if (!File.Exists(file)) file = Path.Combine(TestPaths.RefDir, "adapters", c.asm + ".dll");
                if (!File.Exists(file)) { missing.Add(where + " (assembly not in ref/)"); continue; }

                var type = ctx.LoadFromAssemblyPath(file).GetType(c.type, false);
                if (type == null) { missing.Add(where + " (type not found)"); continue; }

                var members = type.GetMember(c.member, All);
                if (members.Length == 0) { missing.Add(where + " (member not found)"); continue; }

                if (!members.Any(m => ContractMatcher.Matches(m, c.shape))) missing.Add(where + " (signature/kind/staticness mismatch)");
            }

            Assert.True(missing.Count == 0,
                "Game contracts broken (verified against " + VerifiedAgainst + "):\n" + string.Join("\n", missing));
        }

        private static bool IsAdapter(string assembly) => assembly == "Backpacks" || assembly == "Jewelcrafting" || assembly == "HipLantern" || assembly == "AdventureBackpacks";

        [SkippableTheory]
        [InlineData("Backpacks", "org.bepinex.plugins.backpacks", "1.3.10")]
        [InlineData("Jewelcrafting", "org.bepinex.plugins.jewelcrafting", "2.0.10")]
        [InlineData("HipLantern", "shudnal.HipLantern", "1.1.12")]
        [InlineData("AdventureBackpacks", "vapok.mods.adventurebackpacks", "2.0.3")]
        public void Foreign_adapter_contracts_match_the_exact_owning_plugin(string name, string guid, string version)
        {
            TestPaths.SkipUnlessRefs(); TestPaths.SkipUnlessPlugin();
            string path = Path.Combine(TestPaths.RefDir, "adapters", name + ".dll");
            Skip.IfNot(File.Exists(path), "Optional exact-version adapter DLL missing: " + path);
            using var ctx = Load(out var plugin);
            var assembly = ctx.LoadFromAssemblyPath(path);
            var metadata = assembly.GetTypes().SelectMany(t => t.GetCustomAttributesData())
                .Single(a => a.AttributeType.FullName == "BepInEx.BepInPlugin" && (string)a.ConstructorArguments[0].Value == guid);
            Assert.Equal(version, (string)metadata.ConstructorArguments[2].Value);
            var declared = Contracts(plugin).Where(c => c.asm == name).ToArray();
            Assert.NotEmpty(declared);
            foreach (var c in declared)
            {
                var type = assembly.GetType(c.type, false);
                Assert.NotNull(type);
                Assert.True(type.GetMember(c.member, All).Any(m => ContractMatcher.Matches(m, c.shape)),
                    name + " " + version + ": " + c.type + "::" + c.member);
            }
        }

        [SkippableTheory]
        [InlineData("Backpacks")]
        [InlineData("Jewelcrafting")]
        public void Item_container_getters_require_the_empty_default_key(string name)
        {
            TestPaths.SkipUnlessRefs(); TestPaths.SkipUnlessPlugin();
            string path = Path.Combine(TestPaths.RefDir, "adapters", name + ".dll");
            Skip.IfNot(File.Exists(path), "Optional adapter DLL missing: " + path);
            using var context = Load(out _);
            var getter = context.LoadFromAssemblyPath(path).GetType("ItemDataManager.ItemInfo", true)
                .GetMethods(All).Single(m => m.Name == "Get" && m.IsGenericMethodDefinition);
            var key = Assert.Single(getter.GetParameters());
            Assert.Equal("System.String", key.ParameterType.FullName);
            Assert.True(key.HasDefaultValue);
            Assert.Equal("", key.RawDefaultValue);
            // Null would resolve the distinct type# entry in the owning mods' class-key rules.
        }

        [SkippableFact]
        public void Helper_contract_dependencies_do_not_import_other_module_lifecycles()
        {
            TestPaths.SkipUnlessRefs(); TestPaths.SkipUnlessPlugin();
            using var context = Load(out var plugin);
            var coupled = new List<string>();
            foreach (var owner in plugin.GetTypes())
            foreach (var attribute in owner.GetCustomAttributesData().Where(a => a.AttributeType.Name == "ContractDependencyAttribute"))
            foreach (var argument in (IReadOnlyCollection<CustomAttributeTypedArgument>)attribute.ConstructorArguments[0].Value)
            {
                var target = (Type)argument.Value;
                if (target.GetInterfaces().Any(i => i.FullName == "GenesisUI.Host.IUiModule")) coupled.Add(owner.FullName + " -> " + target.FullName);
            }
            Assert.True(coupled.Count == 0, "Use explicit host prerequisites for module lifecycles; helpers must not disable unrelated modules:\n" + string.Join("\n", coupled));
        }

        [SkippableFact]
        public void Field_contracts_pin_kind_type_and_staticness()
        {
            TestPaths.SkipUnlessRefs(); TestPaths.SkipUnlessPlugin();
            using var context = Load(out var plugin);
            var weak = new List<string>();
            foreach (var contract in Contracts(plugin))
            {
                if (IsAdapter(contract.asm) && !File.Exists(Path.Combine(TestPaths.RefDir, "adapters", contract.asm + ".dll"))) continue;
                var type = context.LoadFromAssemblyName(contract.asm).GetType(contract.type, false);
                if (type == null || type.GetField(contract.member, All) == null) continue;
                if (contract.shape.Kind != ContractMemberKind.Field || contract.shape.Static == ContractStatic.Any || string.IsNullOrEmpty(contract.shape.ValueType)) weak.Add(contract.owner.FullName + " -> " + contract.type + "::" + contract.member);
            }
            Assert.True(weak.Count == 0, "Field contracts must validate reflected/compiled field shape:\n" + string.Join("\n", weak));
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
