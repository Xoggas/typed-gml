using System.Text.Json.Nodes;
using TypedGML.Transpiler;

namespace TypedGML.CLI.GameMaker;

internal sealed class GameMakerProject
{
    private const string GeneratedRootFolderName = "TypedGML";
    private const string GeneratedScriptsFolderName = "TypedGML_Scripts";
    private const string GeneratedObjectsFolderName = "TypedGML_Objects";
    private const string GeneratedRootFolderPath = "folders/TypedGML.yy";
    private const string GeneratedScriptsFolderPath = "folders/TypedGML/TypedGML_Scripts.yy";
    private const string GeneratedObjectsFolderPath = "folders/TypedGML/TypedGML_Objects.yy";

    private readonly string _projectFilePath;
    private readonly string _resourceOrderFilePath;
    private readonly JsonObject _projectJson;
    private readonly JsonObject _resourceOrderJson;

    private GameMakerProject(string projectRootPath, string projectFilePath, string resourceOrderFilePath, JsonObject projectJson, JsonObject resourceOrderJson)
    {
        ProjectRootPath = projectRootPath;
        _projectFilePath = projectFilePath;
        _resourceOrderFilePath = resourceOrderFilePath;
        _projectJson = projectJson;
        _resourceOrderJson = resourceOrderJson;
    }

    public string ProjectRootPath { get; }

    public string TypedGmlSourcePath => Path.Combine(ProjectRootPath, "tgml_source");

    public static GameMakerProject Open(string path)
    {
        var projectRootPath = ResolveProjectRoot(path);
        var projectFilePath = Directory
            .EnumerateFiles(projectRootPath, "*.yyp", SearchOption.TopDirectoryOnly)
            .OrderBy(static file => file, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        if (projectFilePath is null)
            throw new FileNotFoundException("Could not find a GameMaker .yyp project file.", path);

        var resourceOrderFilePath = Path.Combine(projectRootPath, Path.GetFileNameWithoutExtension(projectFilePath) + ".resource_order");
        var projectJson = GameMakerJson.LoadObject(projectFilePath);
        var resourceOrderJson = GameMakerJson.LoadObjectOrCreate(resourceOrderFilePath);

        return new GameMakerProject(projectRootPath, projectFilePath, resourceOrderFilePath, projectJson, resourceOrderJson);
    }

    public void EnsureTypedGmlSourceStructure()
    {
        Directory.CreateDirectory(TypedGmlSourcePath);
        Directory.CreateDirectory(Path.Combine(TypedGmlSourcePath, "Assets"));
    }

    public void EnsureGeneratedFolders()
    {
        EnsureFolderResource(GeneratedRootFolderName, GeneratedRootFolderPath, parent: null);
        EnsureFolderResource(GeneratedScriptsFolderName, GeneratedScriptsFolderPath, GeneratedRootFolderPath);
        EnsureFolderResource(GeneratedObjectsFolderName, GeneratedObjectsFolderPath, GeneratedRootFolderPath);
    }

    public void UpsertScript(GeneratedFile file)
    {
        var parts = file.Path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3)
            throw new InvalidOperationException($"Unexpected script output path '{file.Path}'.");

        var scriptName = parts[1];
        var scriptDirectoryPath = Path.Combine(ProjectRootPath, "scripts", scriptName);
        Directory.CreateDirectory(scriptDirectoryPath);

        File.WriteAllText(Path.Combine(scriptDirectoryPath, parts[^1]), file.Content);

        var yyPath = Path.Combine(scriptDirectoryPath, scriptName + ".yy");
        var scriptJson = File.Exists(yyPath)
            ? GameMakerJson.LoadObject(yyPath)
            : CreateScriptJson(scriptName);

        scriptJson["%Name"] = scriptName;
        scriptJson["name"] = scriptName;
        scriptJson["parent"] ??= GameMakerJson.CreateNamePathReference(GeneratedScriptsFolderName, GeneratedScriptsFolderPath);
        scriptJson["resourceType"] ??= "GMScript";
        scriptJson["resourceVersion"] ??= "2.0";
        scriptJson["isCompatibility"] ??= false;
        scriptJson["isDnD"] ??= false;
        scriptJson["$GMScript"] ??= "v1";

        GameMakerJson.Save(yyPath, scriptJson);
        EnsureResourceEntry(scriptName, $"scripts/{scriptName}/{scriptName}.yy");
        EnsureResourceOrderEntry(scriptName, $"scripts/{scriptName}/{scriptName}.yy");
    }

    public bool TryUpsertObject(string objectName, IReadOnlyList<GeneratedFile> files, out string? error)
    {
        error = null;

        var objectDirectoryPath = Path.Combine(ProjectRootPath, "objects", objectName);
        Directory.CreateDirectory(objectDirectoryPath);

        var desiredEvents = new List<GameMakerEventInfo>();
        foreach (var file in files)
        {
            var generatedFileName = Path.GetFileName(file.Path.Replace('/', Path.DirectorySeparatorChar));
            if (GameMakerEventInfo.TryFromGeneratedFileName(generatedFileName, out var eventInfo) is false || eventInfo is null)
            {
                error = $"Unsupported object event output '{file.Path}'.";
                return false;
            }

            desiredEvents.Add(eventInfo);
            File.WriteAllText(Path.Combine(objectDirectoryPath, eventInfo.PhysicalFileName), file.Content);
        }

        DeleteStaleManagedObjectEventFiles(objectDirectoryPath, desiredEvents);

        var yyPath = Path.Combine(objectDirectoryPath, objectName + ".yy");
        var objectJson = File.Exists(yyPath)
            ? GameMakerJson.LoadObject(yyPath)
            : CreateObjectJson(objectName);

        objectJson["%Name"] = objectName;
        objectJson["name"] = objectName;
        objectJson["parent"] ??= GameMakerJson.CreateNamePathReference(GeneratedObjectsFolderName, GeneratedObjectsFolderPath);
        objectJson["resourceType"] ??= "GMObject";
        objectJson["resourceVersion"] ??= "2.0";
        objectJson["managed"] ??= true;

        var eventList = GameMakerJson.GetOrCreateArray(objectJson, "eventList");
        RemoveManagedEvents(eventList);
        foreach (var eventInfo in desiredEvents
                     .Distinct()
                     .OrderBy(static info => info.EventType)
                     .ThenBy(static info => info.EventNum))
        {
            eventList.Add(CreateEventJson(eventInfo));
        }

        GameMakerJson.Save(yyPath, objectJson);
        EnsureResourceEntry(objectName, $"objects/{objectName}/{objectName}.yy");
        EnsureResourceOrderEntry(objectName, $"objects/{objectName}/{objectName}.yy");
        return true;
    }

    public void Save()
    {
        GameMakerJson.Save(_projectFilePath, _projectJson);
        GameMakerJson.Save(_resourceOrderFilePath, _resourceOrderJson);
    }

    private static string ResolveProjectRoot(string path)
    {
        if (File.Exists(path) && string.Equals(Path.GetExtension(path), ".yyp", StringComparison.OrdinalIgnoreCase))
            return Path.GetDirectoryName(Path.GetFullPath(path)) ?? throw new InvalidOperationException();

        if (Directory.Exists(path))
            return Path.GetFullPath(path);

        throw new DirectoryNotFoundException($"Project path '{path}' does not exist.");
    }

    private void EnsureFolderResource(string folderName, string folderPath, string? parent)
    {
        var folders = GameMakerJson.GetOrCreateArray(_projectJson, "Folders");
        if (folders.OfType<JsonObject>().Any(folder => string.Equals(GameMakerJson.GetString(folder, "folderPath"), folderPath, StringComparison.OrdinalIgnoreCase)) is false)
        {
            folders.Add(new JsonObject
            {
                ["$GMFolder"] = "",
                ["%Name"] = folderName,
                ["folderPath"] = folderPath,
                ["name"] = folderName,
                ["resourceType"] = "GMFolder",
                ["resourceVersion"] = "2.0"
            });
        }

        EnsureFolderOrderEntry(folderName, folderPath);

        var absoluteFolderPath = Path.Combine(ProjectRootPath, folderPath.Replace('/', Path.DirectorySeparatorChar));
        var folderJson = new JsonObject
        {
            ["$GMFolder"] = "",
            ["%Name"] = folderName,
            ["folderPath"] = folderPath,
            ["name"] = folderName,
            ["resourceType"] = "GMFolder",
            ["resourceVersion"] = "2.0"
        };

        if (parent is not null)
            folderJson["parent"] = GameMakerJson.CreateNamePathReference(Path.GetFileNameWithoutExtension(parent), parent);

        GameMakerJson.Save(absoluteFolderPath, folderJson);
    }

    private void EnsureResourceEntry(string name, string path)
    {
        var resources = GameMakerJson.GetOrCreateArray(_projectJson, "resources");
        if (resources.OfType<JsonObject>().Any(resource =>
            resource["id"] is JsonObject id &&
            string.Equals(GameMakerJson.GetString(id, "path"), path, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        resources.Add(new JsonObject
        {
            ["id"] = GameMakerJson.CreateNamePathReference(name, path)
        });
    }

    private void EnsureResourceOrderEntry(string name, string path)
    {
        var resourceOrderSettings = GameMakerJson.GetOrCreateArray(_resourceOrderJson, "ResourceOrderSettings");
        if (resourceOrderSettings.OfType<JsonObject>().Any(setting => string.Equals(GameMakerJson.GetString(setting, "path"), path, StringComparison.OrdinalIgnoreCase)))
            return;

        resourceOrderSettings.Add(new JsonObject
        {
            ["name"] = name,
            ["order"] = NextOrder(resourceOrderSettings),
            ["path"] = path
        });
    }

    private void EnsureFolderOrderEntry(string name, string path)
    {
        var folderOrderSettings = GameMakerJson.GetOrCreateArray(_resourceOrderJson, "FolderOrderSettings");
        if (folderOrderSettings.OfType<JsonObject>().Any(setting => string.Equals(GameMakerJson.GetString(setting, "path"), path, StringComparison.OrdinalIgnoreCase)))
            return;

        folderOrderSettings.Add(new JsonObject
        {
            ["name"] = name,
            ["order"] = NextOrder(folderOrderSettings),
            ["path"] = path
        });
    }

    private static int NextOrder(JsonArray settings)
    {
        return settings.OfType<JsonObject>()
            .Select(setting => GameMakerJson.GetInt32(setting, "order"))
            .DefaultIfEmpty(0)
            .Max() + 1;
    }

    private static JsonObject CreateScriptJson(string scriptName)
    {
        return new JsonObject
        {
            ["$GMScript"] = "v1",
            ["%Name"] = scriptName,
            ["isCompatibility"] = false,
            ["isDnD"] = false,
            ["name"] = scriptName,
            ["parent"] = GameMakerJson.CreateNamePathReference(GeneratedScriptsFolderName, GeneratedScriptsFolderPath),
            ["resourceType"] = "GMScript",
            ["resourceVersion"] = "2.0"
        };
    }

    private static JsonObject CreateObjectJson(string objectName)
    {
        return new JsonObject
        {
            ["$GMObject"] = "",
            ["%Name"] = objectName,
            ["eventList"] = new JsonArray(),
            ["managed"] = true,
            ["name"] = objectName,
            ["overriddenProperties"] = new JsonArray(),
            ["parent"] = GameMakerJson.CreateNamePathReference(GeneratedObjectsFolderName, GeneratedObjectsFolderPath),
            ["parentObjectId"] = null,
            ["persistent"] = false,
            ["physicsAngularDamping"] = 0.1,
            ["physicsDensity"] = 0.5,
            ["physicsFriction"] = 0.2,
            ["physicsGroup"] = 1,
            ["physicsKinematic"] = false,
            ["physicsLinearDamping"] = 0.1,
            ["physicsObject"] = false,
            ["physicsRestitution"] = 0.1,
            ["physicsSensor"] = false,
            ["physicsShape"] = 1,
            ["physicsShapePoints"] = new JsonArray(),
            ["physicsStartAwake"] = true,
            ["properties"] = new JsonArray(),
            ["resourceType"] = "GMObject",
            ["resourceVersion"] = "2.0",
            ["solid"] = false,
            ["spriteId"] = null,
            ["spriteMaskId"] = null,
            ["visible"] = true
        };
    }

    private static JsonObject CreateEventJson(GameMakerEventInfo info)
    {
        return new JsonObject
        {
            ["$GMEvent"] = "v1",
            ["%Name"] = string.Empty,
            ["collisionObjectId"] = null,
            ["eventNum"] = info.EventNum,
            ["eventType"] = info.EventType,
            ["isDnD"] = false,
            ["name"] = string.Empty,
            ["resourceType"] = "GMEvent",
            ["resourceVersion"] = "2.0"
        };
    }

    private static void RemoveManagedEvents(JsonArray eventList)
    {
        var managedKeys = GameMakerEventInfo.All
            .Select(static info => (info.EventType, info.EventNum))
            .ToHashSet();

        for (var i = eventList.Count - 1; i >= 0; i--)
        {
            if (eventList[i] is not JsonObject eventJson)
                continue;

            var key = (GameMakerJson.GetInt32(eventJson, "eventType"), GameMakerJson.GetInt32(eventJson, "eventNum"));
            if (managedKeys.Contains(key))
                eventList.RemoveAt(i);
        }
    }

    private static void DeleteStaleManagedObjectEventFiles(string objectDirectoryPath, IReadOnlyCollection<GameMakerEventInfo> desiredEvents)
    {
        var desiredFiles = desiredEvents.Select(static info => info.PhysicalFileName).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var eventInfo in GameMakerEventInfo.All)
        {
            if (desiredFiles.Contains(eventInfo.PhysicalFileName))
                continue;

            var filePath = Path.Combine(objectDirectoryPath, eventInfo.PhysicalFileName);
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
    }
}

