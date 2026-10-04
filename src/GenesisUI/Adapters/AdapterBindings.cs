using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using BepInEx.Bootstrap;
using GenesisUI.Foundation.Contracts;

namespace GenesisUI.Adapters
{
    /// <summary>Bind only the assembly belonging to the declared loaded plugin. No AppDomain API search.</summary>
    internal static class AdapterBindings
    {
        internal static string Check(Type adapter, string guid, string version, out Assembly assembly)
        {
            assembly = null;
            if (!Chainloader.PluginInfos.TryGetValue(guid, out var plugin)) return guid + " is not installed";
            if (plugin.Metadata.Version != new System.Version(version)) return guid + " " + plugin.Metadata.Version + "; supported version: " + version;
            if (plugin.Instance == null) return guid + " has no live plugin instance";
            assembly = plugin.Instance.GetType().Assembly;
            var contracts = adapter.GetCustomAttributes(typeof(GameContractAttribute), false).Cast<GameContractAttribute>().ToArray();
            var foreign = contracts.FirstOrDefault(c => !c.Assembly.StartsWith("assembly_", StringComparison.Ordinal) && c.Assembly != "GenesisUI");
            if (foreign == null || foreign.Assembly != assembly.GetName().Name)
                return guid + " owning assembly does not match the declared foreign contracts";
            foreach (var contract in contracts)
            {
                if (contract.Assembly != assembly.GetName().Name) continue;
                var type = assembly.GetType(contract.Type, false);
                if (type == null || !type.GetMember(contract.Member, All).Any(m => ContractMatcher.Matches(m, contract)))
                    return "missing owning-mod contract " + contract;
            }
            return null;
        }

        internal const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
        internal static MethodInfo Method(Assembly assembly, string type, string member, params Type[] parameters) =>
            assembly.GetType(type, true).GetMethod(member, All, null, parameters, null) ?? throw new MissingMethodException(type, member);
        internal static T Delegate<T>(MethodInfo method) where T : class => System.Delegate.CreateDelegate(typeof(T), method) as T;
        internal static FieldInfo Field(Assembly assembly, string type, string member) =>
            assembly.GetType(type, true).GetField(member, All) ?? throw new MissingFieldException(type, member);

        internal static Func<object, T> ReadField<T>(FieldInfo field)
        {
            var source = Expression.Parameter(typeof(object), "source");
            return Expression.Lambda<Func<object, T>>(Expression.Convert(Expression.Field(field.IsStatic ? null : Expression.Convert(source, field.DeclaringType), field), typeof(T)), source).Compile();
        }

        internal static Func<ItemDrop.ItemData, object> ItemContainer(Assembly assembly, string containerType)
        {
            var item = Expression.Parameter(typeof(ItemDrop.ItemData), "item");
            var data = Method(assembly, "ItemDataManager.ItemExtensions", "Data", typeof(ItemDrop.ItemData));
            var getter = assembly.GetType("ItemDataManager.ItemInfo", true).GetMethods(All)
                .Single(m => m.Name == "Get" && m.IsGenericMethodDefinition).MakeGenericMethod(assembly.GetType(containerType, true));
            var key = getter.GetParameters()[0];
            if (!key.HasDefaultValue || !(key.DefaultValue is string))
                throw new InvalidOperationException(containerType + " has no verified string default key");
            // ItemDataManager treats null as a different key (type + "#"), not its default entry.
            return Expression.Lambda<Func<ItemDrop.ItemData, object>>(Expression.Convert(Expression.Call(Expression.Call(data, item), getter, Expression.Constant(key.DefaultValue, typeof(string))), typeof(object)), item).Compile();
        }

        internal static Func<object, T> ReadMethod<T>(MethodInfo method)
        {
            var source = Expression.Parameter(typeof(object), "source");
            return Expression.Lambda<Func<object, T>>(Expression.Convert(Expression.Call(Expression.Convert(source, method.DeclaringType), method), typeof(T)), source).Compile();
        }
        internal static Func<bool> Toggle(FieldInfo field, string value)
        {
            var config = Expression.Field(null, field);
            var setting = Expression.Property(config, "Value");
            return Expression.Lambda<Func<bool>>(Expression.Equal(setting,
                Expression.Constant(Enum.Parse(setting.Type, value), setting.Type))).Compile();
        }
    }
}
