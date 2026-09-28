using System;

namespace GenesisUI.Foundation.Contracts
{
    /// <summary>
    /// Declares a game member a class depends on (docs/PATCH-POLICY.md rule 6).
    /// Read at runtime by the contract resolver (a missing member disables the
    /// class and everything that declared it) and at test time by
    /// GenesisUI.Contract.Tests against ref/*.dll, before a player ever sees it.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class GameContractAttribute : Attribute
    {
        /// <param name="assembly">File name without extension, e.g. "assembly_valheim".</param>
        /// <param name="type">Full type name, e.g. "MessageHud".</param>
        /// <param name="member">Method, field, property or event name.</param>
        public GameContractAttribute(string assembly, string type, string member)
        {
            Assembly = assembly;
            Type = type;
            Member = member;
        }

        public string Assembly { get; }
        public string Type { get; }
        public string Member { get; }

        /// <summary>
        /// Parameter type names (e.g. "System.String") to pin one method overload.
        /// Null means any member with that name satisfies the contract.
        /// </summary>
        public string[] Parameters { get; set; }

        public override string ToString() =>
            Assembly + ":" + Type + "::" + Member + (Parameters == null ? "" : "(" + string.Join(", ", Parameters) + ")");
    }
}
