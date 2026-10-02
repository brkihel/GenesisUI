using System.Reflection;
using System.Text.Json;

internal static class BuildReceipt
{
    internal static void Run(string repo, string dll)
    {
        var paths = Directory.GetFiles(Path.Combine(repo, "ref"), "*.dll").Append(Path.GetFullPath(dll)).Concat(Directory.GetFiles(Path.GetDirectoryName(typeof(object).Assembly.Location)!, "*.dll"));
        var unique = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in paths) try { unique.TryAdd(AssemblyName.GetAssemblyName(path).Name!, path); } catch (BadImageFormatException) { }
        using var context = new MetadataLoadContext(new PathAssemblyResolver(unique.Values), "mscorlib");
        var plugin = context.LoadFromAssemblyPath(Path.GetFullPath(dll));
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
        object Read(string type, string field) => plugin.GetType(type, true)!.GetField(field, flags)!.GetRawConstantValue()!;
        var channel = Read("GenesisUI.Build", "Channel");
        var enumType = plugin.GetType("GenesisUI.Foundation.Versioning.BuildChannel", true)!;
        string channelName = enumType.GetFields(flags).Single(f => f.IsLiteral && Equals(f.GetRawConstantValue(), channel)).Name;
        Console.WriteLine(JsonSerializer.Serialize(new { version = Read("GenesisUI.PluginInfo", "Version"), preview = Read("GenesisUI.PluginInfo", "PreviewNumber"), sha = Read("GenesisUI.BuildStamp", "Sha"), channel = channelName }));
    }
}
