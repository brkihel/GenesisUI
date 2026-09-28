// Lists members of a type in ref/*.dll through metadata only (nothing is executed).
//
//   dotnet run --project tools/inspect -- <assembly> <Type> [nameFilter]
//   dotnet run --project tools/inspect -- assembly_valheim Hud m_health
//
// Use it before declaring a [GameContract]: it shows the exact name, static-ness,
// visibility and signature the contract test will check.
using System.Reflection;

if (args.Length < 2)
{
    Console.WriteLine("usage: inspect <assembly> <Type> [nameFilter]");
    return 2;
}

string root = AppContext.BaseDirectory;
while (root != null && !File.Exists(Path.Combine(root, "GenesisUI.sln"))) root = Path.GetDirectoryName(root);
string refDir = Path.Combine(root ?? ".", "ref");

var paths = Directory.GetFiles(refDir, "*.dll").ToList();
// Resolution only: the full Managed folder of devplugins, so every Unity module resolves.
string full = Path.Combine(root ?? ".", "..", "devplugins", "referencias", "valheim", "valheim_Data", "Managed");
if (Directory.Exists(full))
{
    var have = new HashSet<string>(paths.Select(Path.GetFileNameWithoutExtension)!, StringComparer.OrdinalIgnoreCase);
    paths.AddRange(Directory.GetFiles(full, "*.dll").Where(p => !have.Contains(Path.GetFileNameWithoutExtension(p))));
}
var names = new HashSet<string>(paths.Select(Path.GetFileNameWithoutExtension)!, StringComparer.OrdinalIgnoreCase);
string runtime = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
paths.AddRange(Directory.GetFiles(runtime, "*.dll").Where(p => !names.Contains(Path.GetFileNameWithoutExtension(p))));

using var ctx = new MetadataLoadContext(new PathAssemblyResolver(paths), "mscorlib");
var type = ctx.LoadFromAssemblyPath(Path.Combine(refDir, args[0] + ".dll")).GetType(args[1]);
if (type == null)
{
    Console.WriteLine("type not found: " + args[1]);
    return 1;
}

string filter = args.Length > 2 ? args[2] : "";
const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
Console.WriteLine(type.FullName + " : " + type.BaseType?.FullName);
foreach (var m in type.GetMembers(all).Where(m => m.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)).OrderBy(m => m.Name))
{
    string line;
    try
    {
    line = m switch
    {
        MethodInfo mi => (mi.IsStatic ? "static " : "") + (mi.IsPublic ? "public " : "private ") + mi.ReturnType.Name + " " + mi.Name
                         + "(" + string.Join(", ", mi.GetParameters().Select(p => p.ParameterType.FullName + " " + p.Name)) + ")",
        FieldInfo fi => (fi.IsStatic ? "static " : "") + (fi.IsPublic ? "public " : "private ") + "field " + fi.FieldType.Name + " " + fi.Name,
        PropertyInfo pi => "prop " + pi.PropertyType.Name + " " + pi.Name,
        EventInfo ei => "event " + ei.EventHandlerType?.Name + " " + ei.Name,
        ConstructorInfo => "",
        _ => m.MemberType + " " + m.Name,
    };
    }
    catch (Exception e)
    {
        line = m.MemberType + " " + m.Name + "  (unresolved: " + e.GetType().Name + ")";
    }
    if (line.Length > 0) Console.WriteLine("  " + line);
}
return 0;
