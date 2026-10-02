using System.Linq;
using System.Reflection;

namespace GenesisUI.Foundation.Contracts
{
    public static class ContractMatcher
    {
        public static bool Matches(MemberInfo member, GameContractAttribute contract)
        {
            var method = member as MethodBase;
            var field = member as FieldInfo;
            var property = member as PropertyInfo;
            var evt = member as EventInfo;
            if (contract.Kind == ContractMemberKind.Field && field == null || contract.Kind == ContractMemberKind.Method && method == null || contract.Kind == ContractMemberKind.Property && property == null || contract.Kind == ContractMemberKind.Event && evt == null) return false;
            if (contract.Parameters != null && (method == null || !method.GetParameters().Select(p => p.ParameterType.FullName).SequenceEqual(contract.Parameters))) return false;
            var accessor = property != null ? property.GetGetMethod(true) ?? property.GetSetMethod(true) : evt != null ? evt.GetAddMethod(true) : null;
            bool? isStatic = field != null ? field.IsStatic : method != null ? method.IsStatic : accessor != null ? accessor.IsStatic : (bool?)null;
            if (contract.Static != ContractStatic.Any && (!isStatic.HasValue || isStatic.Value != (contract.Static == ContractStatic.Static))) return false;
            var value = field != null ? field.FieldType : property != null ? property.PropertyType : member is MethodInfo info ? info.ReturnType : evt != null ? evt.EventHandlerType : null;
            return contract.ValueType == null || value != null && (value.FullName == contract.ValueType || TypeName(value) == contract.ValueType);
        }
        public static string TypeName(System.Type type) => type.IsByRef ? TypeName(type.GetElementType()) + "&" : type.IsArray ? TypeName(type.GetElementType()) + "[]" : type.IsGenericType ? type.GetGenericTypeDefinition().FullName + "<" + string.Join(",", type.GetGenericArguments().Select(TypeName)) + ">" : type.FullName;
    }
}
