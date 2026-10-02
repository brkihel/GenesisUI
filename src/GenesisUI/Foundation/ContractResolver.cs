using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GenesisUI.Foundation.Contracts;

namespace GenesisUI.Foundation
{
    /// <summary>
    /// Checks [GameContract] declarations against the assemblies loaded in the game.
    /// Runs once per class; the result decides whether the class may be used.
    /// </summary>
    [GenesisUI.Foundation.Contracts.ContractDependency(typeof(GenesisUI.Foundation.Contracts.ContractDependencyAttribute), typeof(GenesisUI.Foundation.Contracts.GameContractAttribute), typeof(GenesisUI.Foundation.Contracts.ContractMatcher))]
    internal static class ContractResolver
    {
        private const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        /// <returns>One human-readable line per missing member; empty when everything resolved.</returns>
        public static IReadOnlyList<string> Missing(Type declaring)
        {
            var missing = new List<string>();
            Collect(declaring, new HashSet<Type>(), missing);
            return missing;
        }
        private static void Collect(Type declaring, HashSet<Type> seen, List<string> missing)
        {
            if (declaring == null || !seen.Add(declaring)) return;
            foreach (var c in declaring.GetCustomAttributes(typeof(GameContractAttribute), false).Cast<GameContractAttribute>())
            {
                if (!Resolve(c, out string reason)) missing.Add(c + " — " + reason);
            }
            if (declaring.BaseType != null && declaring.BaseType.Assembly == declaring.Assembly) Collect(declaring.BaseType, seen, missing);
            foreach (var nested in declaring.GetNestedTypes(All)) Collect(nested, seen, missing);
            foreach (var dependency in declaring.GetCustomAttributes(typeof(ContractDependencyAttribute), false).Cast<ContractDependencyAttribute>())
                foreach (var type in dependency.Types) Collect(type, seen, missing);
        }

        public static bool Resolve(GameContractAttribute c, out string reason)
        {
            var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == c.Assembly);
            if (assembly == null)
            {
                reason = "assembly not loaded";
                return false;
            }

            var type = assembly.GetType(c.Type, false);
            if (type == null)
            {
                reason = "type not found";
                return false;
            }

            var members = type.GetMember(c.Member, All);
            if (members.Length == 0)
            {
                reason = "member not found";
                return false;
            }

            if (!members.Any(m => ContractMatcher.Matches(m, c)))
            {
                reason = "member signature/kind/staticness changed";
                return false;
            }

            reason = null;
            return true;
        }
    }
}
