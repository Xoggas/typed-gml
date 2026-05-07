namespace TypedGML.CLI;

internal sealed class CliCompileResult
{
    private CliCompileResult(
        IReadOnlyDictionary<string, string> scripts,
        IReadOnlyList<CompiledObjectResource> objects,
        IReadOnlyDictionary<string, string> resourceNamespaces,
        IReadOnlyDictionary<string, string> resourceSourcePaths,
        IReadOnlyList<string> namespaces,
        IReadOnlySet<string> bclScriptNames)
    {
        Scripts = scripts;
        Objects = objects;
        ResourceNamespaces = resourceNamespaces;
        ResourceSourcePaths = resourceSourcePaths;
        Namespaces = namespaces;
        BclScriptNames = bclScriptNames;
    }

    public IReadOnlyDictionary<string, string> Scripts { get; }

    public IReadOnlyList<CompiledObjectResource> Objects { get; }

    public IReadOnlyDictionary<string, string> ResourceNamespaces { get; }

    public IReadOnlyDictionary<string, string> ResourceSourcePaths { get; }

    public IReadOnlyList<string> Namespaces { get; }

    public IReadOnlySet<string> BclScriptNames { get; }

    public static CliCompileResult FromOutput(
        IReadOnlyDictionary<string, string> output,
        string outputRoot,
        IReadOnlyDictionary<string, string> resourceNamespaces,
        IReadOnlyDictionary<string, string> resourceSourcePaths,
        IReadOnlySet<string> bclScriptCandidates)
    {
        return FromOutput(output, outputRoot, resourceNamespaces, resourceSourcePaths, bclScriptCandidates, []);
    }

    public static CliCompileResult FromOutput(
        IReadOnlyDictionary<string, string> output,
        string outputRoot,
        IReadOnlyDictionary<string, string> resourceNamespaces,
        IReadOnlyDictionary<string, string> resourceSourcePaths,
        IReadOnlySet<string> bclScriptCandidates,
        IReadOnlyList<string> objectNames)
    {
        var scripts = new SortedDictionary<string, string>(StringComparer.Ordinal);
        var objects = new SortedDictionary<string, SortedDictionary<string, string>>(StringComparer.Ordinal);

        foreach (var (path, content) in output)
            AddOutput(path, content, outputRoot, scripts, objects);
        foreach (var objectName in objectNames)
            objects.TryAdd(objectName, new SortedDictionary<string, string>(StringComparer.Ordinal));

        var objectResources = objects
            .Select(entry => new CompiledObjectResource(
                entry.Key,
                entry.Value,
                SourcePath(entry.Key, resourceSourcePaths)))
            .ToList();
        var namespaces = BuildNamespaces(resourceNamespaces, scripts.Keys);
        var bclScriptNames = scripts.Keys
            .Where(bclScriptCandidates.Contains)
            .ToHashSet(StringComparer.Ordinal);

        return new CliCompileResult(scripts, objectResources, resourceNamespaces, resourceSourcePaths, namespaces, bclScriptNames);
    }

    private static void AddOutput(
        string path,
        string content,
        string outputRoot,
        SortedDictionary<string, string> scripts,
        SortedDictionary<string, SortedDictionary<string, string>> objects)
    {
        var relative = Path.GetRelativePath(outputRoot, path);
        var parts = relative.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2 && parts[0] == "scripts" && Path.GetExtension(parts[^1]) == ".gml")
        {
            scripts[Path.GetFileNameWithoutExtension(parts[^1])] = content;
            return;
        }

        if (parts.Length >= 3 && parts[0] == "objects" && Path.GetExtension(parts[^1]) == ".gml")
        {
            if (!objects.TryGetValue(parts[1], out var events))
            {
                events = new SortedDictionary<string, string>(StringComparer.Ordinal);
                objects[parts[1]] = events;
            }

            events[Path.GetFileNameWithoutExtension(parts[^1])] = content;
        }
    }

    private static IReadOnlyList<string> BuildNamespaces(
        IReadOnlyDictionary<string, string> resourceNamespaces,
        IEnumerable<string> scriptNames)
    {
        var namespaces = resourceNamespaces.Values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToHashSet(StringComparer.Ordinal);

        if (scriptNames.Any(name => name.StartsWith("TypedGML_", StringComparison.Ordinal)))
            namespaces.Add("TypedGML");

        return namespaces.Order(StringComparer.Ordinal).ToList();
    }

    private static string? SourcePath(
        string resourceName,
        IReadOnlyDictionary<string, string> resourceSourcePaths) =>
        resourceSourcePaths.TryGetValue(resourceName, out var sourcePath) ? sourcePath : null;
}
