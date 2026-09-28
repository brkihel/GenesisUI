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
    internal static class ContractResolver
    {
        private const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        /// <returns>One human-readable line per missing member; empty when everything resolved.</returns>
        public static IReadOnlyList<string> Missing(Type declaring)
        {
            var missing = new List<string>();
            foreach (var c in declaring.GetCustomAttributes(typeof(GameContractAttribute), false).Cast<GameContractAttribute>())
            {
                if (!Resolve(c, out string reason)) missing.Add(c + " — " + reason);
            }
            return missing;
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

            if (c.Parameters != null)
            {
                bool any = members.OfType<MethodBase>().Any(m =>
                    m.GetParameters().Select(p => p.ParameterType.FullName).SequenceEqual(c.Parameters));
                if (!any)
                {
                    reason = "no overload with parameters (" + string.Join(", ", c.Parameters) + ")";
                    return false;
                }
            }

            reason = null;
            return true;
        }
    }
}
