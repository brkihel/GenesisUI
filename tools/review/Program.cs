// Read-only evidence generator. MetadataLoadContext never executes plugin/game code.
using System.Reflection;
using System.Text.Json;
using GenesisUI.Contract.Tests;
using GenesisUI.Foundation.Faults;
using GenesisUI.InventoryModel;

if (args.Length == 2 && args[0] == "contracts") { ContractAudit.Run(args[1]); return; }
if (args.Length == 3 && args[0] == "stamp") { BuildReceipt.Run(args[1], args[2]); return; }
if (args.Length < 3 || args.Length > 4) throw new ArgumentException("review <repo> <Gale-profile> <output-directory> [Valheim-Managed-directory]");
string repo = Path.GetFullPath(args[0]), profile = Path.GetFullPath(args[1]), output = Path.GetFullPath(args[2]);
Directory.CreateDirectory(output);
string plugin = Path.Combine(repo, "src/GenesisUI/bin/Preview/net48/GenesisUI.dll");
string managed = args.Length == 4 ? Path.GetFullPath(args[3]) : @"C:\Program Files (x86)\Steam\steamapps\common\Valheim\valheim_Data\Managed";
var foreign = Directory.GetFiles(Path.Combine(profile, "BepInEx/plugins"), "*.dll", SearchOption.AllDirectories);
var candidates = Directory.GetFiles(Path.Combine(repo, "ref"), "*.dll").Concat(foreign)
    .Concat(Directory.Exists(managed) ? Directory.GetFiles(managed, "*.dll") : Array.Empty<string>())
    .Concat(Directory.GetFiles(Path.Combine(profile, "BepInEx/core"), "*.dll"))
    .Concat(Directory.GetFiles(Path.GetDirectoryName(typeof(object).Assembly.Location)!, "*.dll"))
    .Append(plugin);
var paths = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
foreach (var file in candidates)
    try { paths.TryAdd(AssemblyName.GetAssemblyName(file).Name!, file); } catch (BadImageFormatException) { }
using var context = new MetadataLoadContext(new PathAssemblyResolver(paths.Values), "mscorlib");
var contracts = new List<object>();
var declared = new HashSet<string>();
var own = context.LoadFromAssemblyPath(plugin);
foreach (var type in own.GetTypes())
foreach (var attribute in type.GetCustomAttributesData().Where(a => a.AttributeType.Name == "GameContractAttribute"))
{
    var a = attribute.ConstructorArguments;
    contracts.Add(new { Owner=type.FullName, Assembly=a[0].Value, Type=a[1].Value, Member=a[2].Value });
    declared.Add(a[1].Value + "::" + a[2].Value);
}
var gameTypes = new HashSet<string>();
foreach (var file in new[]{"assembly_valheim", "assembly_utils", "assembly_guiutils", "gui_framework"})
    foreach(var type in context.LoadFromAssemblyPath(paths[file]).GetTypes()) gameTypes.Add(type.FullName!);
var references = IlScanner.Scan(plugin).Where(r => r.CallerType.StartsWith("GenesisUI.") && !r.Target.StartsWith("type:")
    && !r.Target.StartsWith("<PrivateImplementationDetails>")
    && gameTypes.Contains(r.Target.Split("::")[0].Replace('/', '+'))).ToList();
var absent = references.Where(r => !declared.Contains(r.Target.Replace('/', '+')))
    .Select(r=>new {r.CallerType,r.CallerMethod,r.Target}).Distinct().ToList();
void Save(string name, object value) => File.WriteAllText(Path.Combine(output,name),JsonSerializer.Serialize(value,new JsonSerializerOptions{WriteIndented=true,IncludeFields=true}));
Save("contracts.json",contracts); Save("game-references-without-global-declaration.json",absent);
if (Directory.Exists(managed))
{
    // Check the installed client independently; never replace production ref/ files.
    var clientPaths = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
    foreach (var file in Directory.GetFiles(managed, "*.dll").Concat(paths.Values))
        try { clientPaths.TryAdd(AssemblyName.GetAssemblyName(file).Name!, file); } catch (BadImageFormatException) { }
    using var client = new MetadataLoadContext(new PathAssemblyResolver(clientPaths.Values), "mscorlib");
    var missing = new List<string>();
    foreach (var type in own.GetTypes())
    foreach (var attribute in type.GetCustomAttributesData().Where(a => a.AttributeType.Name == "GameContractAttribute"))
    {
        var a = attribute.ConstructorArguments;
        string assemblyName=(string)a[0].Value!, typeName=(string)a[1].Value!, memberName=(string)a[2].Value!;
        string label=type.FullName + " -> " + assemblyName + ":" + typeName + "::" + memberName;
        try
        {
            var target = client.LoadFromAssemblyPath(clientPaths[assemblyName]).GetType(typeName);
            var members = target?.GetMember(memberName, BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static);
            if (members == null || members.Length == 0) { missing.Add(label + " (missing)"); continue; }
            var parameters = attribute.NamedArguments.FirstOrDefault(n => n.MemberName == "Parameters").TypedValue.Value
                as IReadOnlyCollection<CustomAttributeTypedArgument>;
            if (parameters != null && !members.OfType<MethodBase>().Any(m =>
                m.GetParameters().Select(p => p.ParameterType.FullName).SequenceEqual(parameters.Select(p => (string)p.Value!))))
                missing.Add(label + " (overload missing)");
        }
        catch (Exception e) { missing.Add(label + " (" + e.GetType().Name + ")"); }
    }
    Save("installed-client-contracts.json",new {Checked=contracts.Count,Missing=missing});
}
var surfaces = new List<object>();
var apiSignatures = new List<object>();
var inspectedTypes = new HashSet<string>(StringComparer.Ordinal)
{
    "Backpacks.API", "Backpacks.ItemContainer", "Jewelcrafting.API",
    "HipLantern.HumanoidExtension", "HipLantern.VisEquipmentExtension",
};
foreach(var file in foreign)
{
    try
    {
        var assembly = context.LoadFromAssemblyPath(file);
        var types = assembly.GetTypes();
        foreach (var type in types.Where(t => inspectedTypes.Contains(t.FullName!)))
        {
            var members = new List<string>();
            foreach (var member in type.GetMembers(BindingFlags.Public|BindingFlags.Instance|BindingFlags.Static|BindingFlags.DeclaredOnly))
            {
                try
                {
                    if (member is MethodInfo method)
                        members.Add((method.IsStatic ? "static " : "instance ") + method.ReturnType.FullName + " " + method.Name +
                            "(" + string.Join(", ", method.GetParameters().Select(p => p.ParameterType.FullName + " " + p.Name)) + ")");
                    else if (member is FieldInfo field)
                        members.Add((field.IsStatic ? "static " : "instance ") + "field " + field.FieldType.FullName + " " + field.Name);
                }
                catch (Exception e) { members.Add(member.Name + " [unresolved: " + e.GetType().Name + "]"); }
            }
            apiSignatures.Add(new { File=Path.GetRelativePath(Path.Combine(profile,"BepInEx/plugins"),file), Type=type.FullName, Members=members });
        }
        var plugins = new List<object>();
        foreach(var type in types)
        {
            foreach(var a in type.GetCustomAttributesData().Where(a=>a.AttributeType.Name=="BepInPlugin"))
                plugins.Add(new {Type=type.FullName,Guid=a.ConstructorArguments[0].Value,Name=a.ConstructorArguments[1].Value,Version=a.ConstructorArguments[2].Value});
        }
        // Names are candidate surfaces only; not supported API/signature evidence.
        var interesting = types.Where(t=>System.Text.RegularExpressions.Regex.IsMatch(t.FullName!,"Backpack|Socket|Gem|Lantern|Equipment|Inventory|Tooltip|Hud|Achievement|Season|Magic|Skill|UI|Sagas",System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            .Select(t=>new {Type=t.FullName,Members=t.GetMembers(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance|BindingFlags.DeclaredOnly).Select(m=>m.Name).Distinct().ToArray()}).ToList();
        surfaces.Add(new {File=Path.GetRelativePath(Path.Combine(profile,"BepInEx/plugins"),file),Assembly=assembly.GetName().Name,Plugins=plugins,Candidates=interesting});
    }
    catch(Exception e) {surfaces.Add(new {File=Path.GetRelativePath(Path.Combine(profile,"BepInEx/plugins"),file),Error=e.GetType().Name,Message=e.Message});}
}
Save("modpack-metadata-surfaces.json",surfaces);
Save("selected-public-surfaces.json",apiSignatures);

// Reproduce host-before-cleanup subscription order without Unity or game code.
var faults=new FaultRegistry(); var events=new List<string>(); bool veil=false, lease=false;
faults.Tripped += f=>{events.Add("host: teardown old -> rebuild new"); faults.Reset(f.Owner); veil=true; lease=true;};
faults.Tripped += f=>{events.Add("veil subscriber: restore owner's newly built veil"); veil=false;};
faults.Tripped += f=>{events.Add("input subscriber: release owner's newly acquired lease"); lease=false;};
faults.Report("module:example","fault","detail",0);
var layout=new SlotLayout(4,4,4,new[]{EquipSlot.Head});
var duplicateId=LayoutChange.Compute(layout,layout,new[]{new ItemAt(0,0,0),new ItemAt(0,1,0)});
var numberGrammar = new List<object>();
foreach (var input in new[]{"01", "1.", "-.1"})
{
    try { numberGrammar.Add(new {Input=input,Accepted=true,Value=GenesisUI.Data.StrictJson.Parse(input)}); }
    catch (FormatException) { numberGrammar.Add(new {Input=input,Accepted=false,Value=(object)null}); }
}
Save("pure-reproductions.json",new {RecoveryOrder=new {Events=events,NewVeilSurvives=veil,NewLeaseSurvives=lease},DuplicateIdPlan=new {duplicateId.Ok,duplicateId.Moves},InvalidJsonNumbers=numberGrammar});
Console.WriteLine($"contracts {contracts.Count}; undeclared global game references {absent.Count}; metadata files {surfaces.Count}; recovery veil {veil}, lease {lease}; duplicate-id plan accepted {duplicateId.Ok}");
