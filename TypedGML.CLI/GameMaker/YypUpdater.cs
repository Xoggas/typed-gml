using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace TypedGML.CLI.GameMaker;

internal sealed class YypUpdater
{
    public void Update(
        string yypFilePath,
        IReadOnlyList<string> scriptNames,
        IReadOnlyList<string> objectNames,
        IReadOnlyList<FolderEntry> folders)
    {
        _ = folders;
        var originalText = LoadProjectText(yypFilePath);
        var resources = LoadResources(originalText);
        UpdateResources(resources, scriptNames, objectNames);
        SaveProject(yypFilePath, ReplaceResources(originalText, resources));
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

    private static JsonArray LoadResources(string projectText)
    {
        var cleaned = Regex.Replace(projectText, @",(\s*[}\]])", "$1");
        using var document = JsonDocument.Parse(cleaned);
        var root = JsonNode.Parse(document.RootElement.GetRawText()) as JsonObject;
        return root?["resources"] as JsonArray ?? [];
    }

    private static void UpdateResources(
        JsonArray resources,
        IReadOnlyList<string> scriptNames,
        IReadOnlyList<string> objectNames)
    {
        var generatedNames = scriptNames.Concat(objectNames).ToHashSet(StringComparer.Ordinal);
        var retainedResources = resources
            .Where(resource => ShouldKeepResource(resource, generatedNames))
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

    private static bool ShouldKeepResource(JsonNode? resource, HashSet<string> generatedNames)
    {
        var name = StringValue(resource?["id"]?["name"]);
        var path = StringValue(resource?["id"]?["path"]);
        if (name is null || path is null)
            return true;

        return !generatedNames.Contains(name) ||
            (!path.StartsWith("scripts/", StringComparison.Ordinal) &&
             !path.StartsWith("objects/", StringComparison.Ordinal));
    }

    private static string ReplaceResources(string projectText, JsonArray resources)
    {
        var replacement = new YypResourceArrayWriter().Write(resources);
        if (YypResourceArraySpan.TryFind(projectText, out var span))
            return projectText[..span.Start] + replacement + projectText[(span.End + 1)..];

        var insertionPoint = projectText.LastIndexOf('}');
        if (insertionPoint < 0)
            return $"{{\n  \"resources\":{replacement},\n}}\n";

        var prefix = projectText[..insertionPoint].TrimEnd();
        return $"{prefix}\n  \"resources\":{replacement},\n{projectText[insertionPoint..]}";
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
