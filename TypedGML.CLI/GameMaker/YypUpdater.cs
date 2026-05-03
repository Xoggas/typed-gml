using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace TypedGML.CLI.GameMaker;

internal class YypUpdater
{
    public void Update(
        string yypFilePath,
        IReadOnlyList<string> scriptNames,
        IReadOnlyList<string> objectNames,
        IReadOnlyList<FolderEntry> folders)
    {
        var root = LoadProject(yypFilePath);
        UpdateResources(GetOrCreateArray(root, "resources"), scriptNames, objectNames);
        UpdateFolders(GetOrCreateArray(root, "Folders", "folders"), folders);
        SaveProject(yypFilePath, root);
    }

    private static JsonObject LoadProject(string yypFilePath)
    {
        if (!File.Exists(yypFilePath))
            return CreateSkeleton(yypFilePath);

        var text = File.ReadAllText(yypFilePath);
        var cleaned = Regex.Replace(text, @",(\s*[}\]])", "$1");
        using var document = JsonDocument.Parse(cleaned);
        return JsonNode.Parse(document.RootElement.GetRawText()) as JsonObject
            ?? CreateSkeleton(yypFilePath);
    }

    private static JsonObject CreateSkeleton(string yypFilePath) => new()
    {
        ["resourceType"] = "GMProject",
        ["resourceVersion"] = "1.0",
        ["name"] = Path.GetFileNameWithoutExtension(yypFilePath),
        ["resources"] = new JsonArray(),
        ["Folders"] = new JsonArray()
    };

    private static JsonArray GetOrCreateArray(JsonObject root, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (root[propertyName] is JsonArray existing)
                return existing;
        }

        var created = new JsonArray();
        root[propertyNames[0]] = created;
        return created;
    }

    private static void UpdateResources(
        JsonArray resources,
        IReadOnlyList<string> scriptNames,
        IReadOnlyList<string> objectNames)
    {
        var generatedNames = scriptNames.Concat(objectNames).ToHashSet(StringComparer.Ordinal);
        RemoveGeneratedResources(resources, generatedNames);
        foreach (var scriptName in scriptNames.Distinct(StringComparer.Ordinal))
            resources.Add(CreateResource(scriptName, "scripts"));
        foreach (var objectName in objectNames.Distinct(StringComparer.Ordinal))
            resources.Add(CreateResource(objectName, "objects"));
    }

    private static void UpdateFolders(JsonArray folderArray, IReadOnlyList<FolderEntry> folders)
    {
        for (var i = folderArray.Count - 1; i >= 0; i--)
            if (StringValue(folderArray[i]?["folderPath"])?.StartsWith("folders/TypedGML", StringComparison.Ordinal) == true)
                folderArray.RemoveAt(i);

        foreach (var folder in folders)
            folderArray.Add(CreateFolder(folder));
    }

    private static void RemoveGeneratedResources(JsonArray resources, HashSet<string> generatedNames)
    {
        for (var i = resources.Count - 1; i >= 0; i--)
        {
            var name = StringValue(resources[i]?["id"]?["name"]);
            var path = StringValue(resources[i]?["id"]?["path"]);
            if (name is null || path is null)
                continue;

            if ((path.StartsWith("scripts/", StringComparison.Ordinal) && generatedNames.Contains(name)) ||
                (path.StartsWith("objects/", StringComparison.Ordinal) && generatedNames.Contains(name)))
                resources.RemoveAt(i);
        }
    }

    private static JsonObject CreateResource(string name, string resourceFolder) => new()
    {
        ["id"] = new JsonObject
        {
            ["name"] = name,
            ["path"] = $"{resourceFolder}/{name}/{name}.yy"
        },
        ["order"] = 0
    };

    private static JsonObject CreateFolder(FolderEntry folder) => new()
    {
        ["resourceType"] = "GMFolder",
        ["resourceVersion"] = "1.0",
        ["name"] = folder.Name,
        ["folderPath"] = NormalizePath(folder.FolderPath),
        ["order"] = 0,
        ["parent"] = new JsonObject
        {
            ["name"] = folder.ParentName,
            ["path"] = NormalizePath(folder.ParentPath)
        },
        ["tags"] = new JsonArray()
    };

    private static void SaveProject(string yypFilePath, JsonObject root)
    {
        var tmpPath = $"{yypFilePath}.tmp";
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(yypFilePath))!);
        var content = new TrailingCommaJsonWriter().Write(root);
        File.WriteAllText(tmpPath, content, new UTF8Encoding(false));
        File.Move(tmpPath, yypFilePath, true);
    }

    private static string NormalizePath(string path) =>
        path.Replace('\\', '/');

    private static string? StringValue(JsonNode? node) =>
        node?.GetValue<string>();
}
