using System;
using System.Reflection;
using GenesisUI.Foundation.Contracts;
using Xunit;

namespace GenesisUI.Core.Tests
{
    public class ContractMatcherTests
    {
        private sealed class Sample { public static int Value = 1; public string Read(int value) => value.ToString(); }
        [Fact]
        public void Field_contract_rejects_type_kind_and_staticness_changes()
        {
            var field = typeof(Sample).GetField(nameof(Sample.Value));
            var contract = new GameContractAttribute("sample", "Sample", "Value") { Kind = ContractMemberKind.Field, Static = ContractStatic.Static, ValueType = "System.Int32" };
            Assert.True(ContractMatcher.Matches(field, contract));
            contract.Static = ContractStatic.Instance; Assert.False(ContractMatcher.Matches(field, contract));
            contract.Static = ContractStatic.Static; contract.ValueType = "System.String"; Assert.False(ContractMatcher.Matches(field, contract));
            contract.ValueType = "System.Int32"; contract.Kind = ContractMemberKind.Method; Assert.False(ContractMatcher.Matches(field, contract));
        }
        [Fact]
        public void Method_contract_pins_parameters_return_and_instance_shape()
        {
            var method = typeof(Sample).GetMethod(nameof(Sample.Read));
            var contract = new GameContractAttribute("sample", "Sample", "Read") { Parameters = new[] { "System.Int32" }, ValueType = "System.String", Static = ContractStatic.Instance, Kind = ContractMemberKind.Method };
            Assert.True(ContractMatcher.Matches(method, contract));
            contract.Parameters = Array.Empty<string>(); Assert.False(ContractMatcher.Matches(method, contract));
            contract.Parameters = new[] { "System.Int32" }; contract.ValueType = "System.Boolean"; Assert.False(ContractMatcher.Matches(method, contract));
        }
    }
}
