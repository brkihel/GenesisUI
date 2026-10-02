using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;
using GenesisUI.Foundation.Contracts;

namespace GenesisUI.Contract.Tests
{
    public class ContractCoverageTests
    {
        private const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        private static string TypeName(Type type) => type.IsByRef ? TypeName(type.GetElementType()) + "&" : type.IsArray ? TypeName(type.GetElementType()) + "[]" : type.IsGenericType ? type.GetGenericTypeDefinition().FullName + "<" + string.Join(",", type.GetGenericArguments().Select(TypeName)) + ">" : type.FullName;
        private static string Normalize(MemberInfo member) => member.DeclaringType.FullName + "::" + member.Name + (member is MethodBase method ? "(" + string.Join(",", method.GetParameters().Select(p => TypeName(p.ParameterType))) + ")" : "");
        [SkippableFact]
        public void Compiled_game_member_usage_has_an_owner_contract()
        {
            TestPaths.SkipUnlessRefs(); TestPaths.SkipUnlessPlugin();
            using var context = GameContractTests.Load(out var plugin);
            var native = new Dictionary<string, Type>();
            foreach (var name in new[] { "assembly_valheim", "assembly_utils", "assembly_guiutils", "gui_framework" })
                foreach (var type in context.LoadFromAssemblyName(name).GetTypes()) native[type.FullName] = type;
            var own = plugin.GetTypes().ToDictionary(t => t.FullName);
            var contracts = new Dictionary<string, HashSet<string>>();
            foreach (var contract in GameContractTests.Contracts(plugin))
            {
                string root = contract.owner.FullName.Split('+')[0];
                if (!contracts.TryGetValue(root, out var declared)) contracts[root] = declared = new HashSet<string>();
                if (!native.TryGetValue(contract.type, out var type)) continue;
                var candidates = contract.member == ".ctor" ? type.GetConstructors(All).Cast<MemberInfo>() : type.GetMember(contract.member, All);
                foreach (var member in candidates.Where(m => ContractMatcher.Matches(m, contract.shape)))
                {
                    declared.Add(Normalize(member));
                    if (member is PropertyInfo property)
                    {
                        if (property.GetGetMethod(true) is MethodInfo getter) declared.Add(Normalize(getter));
                        if (property.GetSetMethod(true) is MethodInfo setter) declared.Add(Normalize(setter));
                    }
                }
            }
            var missing = new List<string>();
            foreach (var reference in IlScanner.Scan(TestPaths.PluginDll).Where(r => r.CallerType.StartsWith("GenesisUI.") && !r.Target.StartsWith("type:") && !r.Target.StartsWith("<PrivateImplementationDetails>")))
            {
                var target = reference.Target.Replace('/', '+').Split("::");
                if (target.Length != 2 || !native.TryGetValue(target[0], out var type)) continue;
                string owner = reference.CallerType.Split('/')[0];
                var members = target[1] == ".ctor" ? type.GetConstructors(All).Cast<MemberInfo>().ToArray() : type.GetMember(target[1], All);
                if (reference.Parameters != null) members = members.OfType<MethodBase>().Where(m => m.GetParameters().Select(p => TypeName(p.ParameterType)).SequenceEqual(reference.Parameters.Select(p => p.Replace('/', '+')))).Cast<MemberInfo>().ToArray();
                if (reference.ReturnType != null) members = members.Where(m => m is ConstructorInfo || m is MethodInfo method && TypeName(method.ReturnType) == reference.ReturnType.Replace('/', '+')).ToArray();
                if (members.Length == 0 || !contracts.TryGetValue(owner, out var declared) || !members.Any(m => declared.Contains(Normalize(m)))) missing.Add(reference.ToString());
            }
            Assert.True(missing.Count == 0, "Undeclared compiled game usage:\n" + string.Join("\n", missing.Distinct()));
        }
    }
}
