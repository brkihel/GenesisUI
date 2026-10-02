using System.Reflection;
using System.Text.Json;
using GenesisUI.Contract.Tests;

internal static class ContractAudit
{
    private const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    internal static string TypeName(Type type) => type.IsByRef ? TypeName(type.GetElementType()!) + "&" : type.IsArray ? TypeName(type.GetElementType()!) + "[]" : type.IsGenericType ? type.GetGenericTypeDefinition().FullName + "<" + string.Join(",", type.GenericTypeArguments.Select(TypeName)) + ">" : type.FullName!;
    internal static void Run(string repo)
    {
        repo = Path.GetFullPath(repo);
        string plugin = Path.Combine(repo, "src/GenesisUI/bin/Debug/net48/GenesisUI.dll");
        var paths = Directory.GetFiles(Path.Combine(repo, "ref"), "*.dll").Append(plugin).Concat(Directory.GetFiles(Path.GetDirectoryName(typeof(object).Assembly.Location)!, "*.dll"));
        var unique = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in paths) try { unique.TryAdd(AssemblyName.GetAssemblyName(path).Name!, path); } catch (BadImageFormatException) { }
        using var context = new MetadataLoadContext(new PathAssemblyResolver(unique.Values), "mscorlib");
        var own = context.LoadFromAssemblyPath(plugin);
        var moduleTypes = new HashSet<string>(own.GetTypes().Where(t => t.GetInterfaces().Any(i => i.FullName == "GenesisUI.Host.IUiModule")).Select(t => t.FullName!));
        var types = new Dictionary<string,Type>();
        foreach (var assembly in new[] { "assembly_valheim", "assembly_utils", "assembly_guiutils", "gui_framework" })
            foreach (var type in context.LoadFromAssemblyPath(unique[assembly]).GetTypes()) types[type.FullName!] = type;
        var declarations = new Dictionary<string,HashSet<string>>();
        var fieldShapes = new Dictionary<string,string>();
        foreach (var owner in own.GetTypes())
        {
            string root = owner.FullName!.Split('+')[0];
            if (!declarations.TryGetValue(root, out var set)) declarations[root] = set = new();
            foreach (var attribute in owner.GetCustomAttributesData().Where(a => a.AttributeType.Name == "GameContractAttribute"))
            {
                string nativeName = (string)attribute.ConstructorArguments[1].Value!, memberName = (string)attribute.ConstructorArguments[2].Value!;
                if (!types.TryGetValue(nativeName, out var native)) continue;
                foreach (var member in native.GetMember(memberName, All))
                {
                    if (member is FieldInfo field) fieldShapes[native.Assembly.GetName().Name + ":" + nativeName + "::" + memberName] = ", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic." + (field.IsStatic ? "Static" : "Instance") + ", ValueType = " + JsonSerializer.Serialize(field.FieldType.FullName);
                    set.Add(member.DeclaringType!.FullName + "::" + member.Name);
                    if (member is PropertyInfo property) { if (property.GetGetMethod(true) is {} getter) set.Add(getter.DeclaringType!.FullName + "::" + getter.Name); if (property.GetSetMethod(true) is {} setter) set.Add(setter.DeclaringType!.FullName + "::" + setter.Name); }
                }
            }
        }
        var additions = new Dictionary<string,HashSet<string>>();
        var dependencies = new Dictionary<string,HashSet<string>>();
        var unresolved = new List<string>();
        foreach (var reference in IlScanner.Scan(plugin).Where(r => r.CallerType.StartsWith("GenesisUI.") && !r.Target.StartsWith("type:")))
        {
            string owner = reference.CallerType.Split('/')[0];
            var parts = reference.Target.Replace('/', '+').Split("::");
            if (parts.Length != 2) continue;
            string target = parts[0];
            if (target.StartsWith("<PrivateImplementationDetails>")) continue;
            if (target.StartsWith("GenesisUI.") && target != owner && target != "GenesisUI.Plugin" && target != "GenesisUI.Host.ModuleHost" && target != "GenesisUI.Diagnostics.Overlay" && owner != "GenesisUI.Plugin" && owner != "GenesisUI.Host.ModuleHost")
            {
                target = target.Split('+')[0];
                if (own.GetType(owner)?.IsClass == true && own.GetType(target)?.IsClass == true && !moduleTypes.Contains(target) && !target.Contains('<') && target != owner) { if (!dependencies.TryGetValue(owner, out var dep)) dependencies[owner] = dep = new(); dep.Add(target); }
            }
            if (!types.TryGetValue(parts[0], out var type)) continue;
            var members = parts[1] == ".ctor" ? type.GetConstructors(All).Cast<MemberInfo>().ToArray() : type.GetMember(parts[1], All);
            if (reference.Parameters != null) members = members.OfType<MethodBase>().Where(m => m.GetParameters().Select(p => TypeName(p.ParameterType)).SequenceEqual(reference.Parameters.Select(p => p.Replace('/', '+')))).Cast<MemberInfo>().ToArray();
            if (members.Length == 0) { unresolved.Add(reference.ToString()); continue; }
            foreach (var member in members)
            {
                string key = member.DeclaringType!.FullName + "::" + member.Name;
                if (declarations.TryGetValue(owner, out var known) && known.Contains(key)) continue;
                if (!additions.TryGetValue(owner, out var rows)) additions[owner] = rows = new();
                var field = member as FieldInfo; var method = member as MethodBase;
                string line = "[GenesisUI.Foundation.Contracts.GameContract(" + JsonSerializer.Serialize(member.DeclaringType.Assembly.GetName().Name) + ", " + JsonSerializer.Serialize(member.DeclaringType.FullName) + ", " + JsonSerializer.Serialize(member.Name);
                if (method != null) line += ", Parameters = new string[] { " + string.Join(", ", method.GetParameters().Select(p => JsonSerializer.Serialize(p.ParameterType.FullName))) + " }, Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Method, Static = GenesisUI.Foundation.Contracts.ContractStatic." + (method.IsStatic ? "Static" : "Instance");
                if (field != null) line += ", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic." + (field.IsStatic ? "Static" : "Instance") + ", ValueType = " + JsonSerializer.Serialize(field.FieldType.FullName);
                if (member is MethodInfo info) line += ", ValueType = " + JsonSerializer.Serialize(info.ReturnType.FullName);
                rows.Add(line + ")]" );
            }
        }
        var output = new { Additions = additions, Dependencies = dependencies, FieldShapes = fieldShapes, ModuleTypes = moduleTypes, Unresolved = unresolved.Distinct().ToArray() };
        string destination = Path.Combine(repo, "dist/contracts-audit.json");
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        File.WriteAllText(destination, JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"Contract audit: {additions.Sum(p=>p.Value.Count)} additions across {additions.Count} owners; unresolved {output.Unresolved.Length}; {destination}");
    }
}
