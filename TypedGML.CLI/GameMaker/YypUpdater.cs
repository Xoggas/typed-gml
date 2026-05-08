using System.Text;
using System.Text.Json.Nodes;

namespace TypedGML.CLI.GameMaker;

internal sealed class YypUpdater
{
    public void Update(
        string yypFilePath,
        IReadOnlyList<string> scriptNames,
        IReadOnlySet<string> bclScriptNames,
        IReadOnlyList<string> objectNames,
        IReadOnlyList<FolderEntry> folders)
    {
        Update(yypFilePath, scriptNames, bclScriptNames, objectNames, folders, GeneratedResourceManifest.CreateEmpty());
    }

    public void Update(
        string yypFilePath,
        IReadOnlyList<string> scriptNames,
        IReadOnlySet<string> bclScriptNames,
        IReadOnlyList<string> objectNames,
        IReadOnlyList<FolderEntry> folders,
        GeneratedResourceManifest previousGeneratedResources)
    {
        var originalText = LoadProjectText(yypFilePath);
        var resources = YypArrayLoader.Load(originalText, "resources");
        UpdateResources(resources, scriptNames, objectNames, previousGeneratedResources);
        var content = YypArrayPropertyReplacer.ReplaceOrAdd(
            originalText,
            "resources",
            new YypResourceArrayWriter().Write(resources));
        content = new YypFolderUpdater().Update(content, originalText, bclScriptNames, folders, previousGeneratedResources.Folders);
        SaveProject(yypFilePath, content);
    }

    private static string LoadProjectText(string yypFilePath)
    {
        if (File.Exists(yypFilePath))
            return File.ReadAllText(yypFilePath).ReplaceLineEndings("\n");

        var projectName = Path.GetFileNameWithoutExtension(yypFilePath);
        return $$"""
        {
          "name":"{{Escape(projectName)}}",
          "resources":[],
          "resourceType":"GMProject",
          "resourceVersion":"2.0",
        }
        """.ReplaceLineEndings("\n");
    }

    private static void UpdateResources(
        JsonArray resources,
        IReadOnlyList<string> scriptNames,
        IReadOnlyList<string> objectNames,
        GeneratedResourceManifest previousGeneratedResources)
    {
        var generatedNames = scriptNames.Concat(objectNames).ToHashSet(StringComparer.Ordinal);
        var staleGeneratedNames = previousGeneratedResources.Scripts
            .Concat(previousGeneratedResources.Objects)
            .Where(name => !generatedNames.Contains(name))
            .ToHashSet(StringComparer.Ordinal);
        var retainedResources = resources
            .Where(resource => ShouldKeepResource(resource, generatedNames, staleGeneratedNames))
            .Select(ResourceEntry.FromJson)
            .OfType<ResourceEntry>()
            .ToList();
        var generatedResources = scriptNames
            .Distinct(StringComparer.Ordinal)
            .Select(name => ResourceEntry.Script(name))
            .Concat(objectNames.Distinct(StringComparer.Ordinal).Select(ResourceEntry.Object));

        resources.Clear();
        foreach (var resource in retainedResources.Concat(generatedResources).OrderBy(resource => resource.Name, StringComparer.Ordinal))
            resources.Add(resource.ToJson());
    }

    private static bool ShouldKeepResource(
        JsonNode? resource,
        HashSet<string> generatedNames,
        HashSet<string> staleGeneratedNames)
    {
        var name = StringValue(resource?["id"]?["name"]);
        var path = StringValue(resource?["id"]?["path"]);
        if (name is null || path is null)
            return true;

        return (!generatedNames.Contains(name) && !staleGeneratedNames.Contains(name)) ||
            (!path.StartsWith("scripts/", StringComparison.Ordinal) &&
             !path.StartsWith("objects/", StringComparison.Ordinal));
    }

    private static void SaveProject(string yypFilePath, string content)
    {
        var tmpPath = $"{yypFilePath}.tmp";
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(yypFilePath))!);
        File.WriteAllText(tmpPath, content.ReplaceLineEndings("\n"), new UTF8Encoding(false));
        File.Move(tmpPath, yypFilePath, true);
    }

    private static string Escape(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);

    private static string? StringValue(JsonNode? node) =>
        node?.GetValue<string>();
}
