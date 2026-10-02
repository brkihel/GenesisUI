using System;

namespace GenesisUI.Foundation.Contracts
{
    /// <summary>Preflight the helper contracts with their caller before Build or patch installation.</summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class ContractDependencyAttribute : Attribute
    {
        public ContractDependencyAttribute(params Type[] types) { Types = types ?? Array.Empty<Type>(); }
        public Type[] Types { get; }
    }
}
