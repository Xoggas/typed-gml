using System.Text.Json;
using System.Text.Json.Nodes;

namespace TypedGML.CLI.GameMaker;

internal static class GameMakerJson
{
    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true
    };

    private static readonly JsonDocumentOptions ReadOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip
    };

    public static JsonObject LoadObject(string path)
    {
        var json = File.ReadAllText(path);
        return JsonNode.Parse(json, documentOptions: ReadOptions)?.AsObject()
               ?? throw new InvalidOperationException($"Failed to parse JSON file '{path}'.");
    }

    public static JsonObject LoadObjectOrCreate(string path)
    {
        return File.Exists(path)
            ? LoadObject(path)
            : new JsonObject();
    }

    public static void Save(string path, JsonObject json)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? throw new InvalidOperationException());
        File.WriteAllText(path, json.ToJsonString(WriteOptions));
    }

    public static JsonArray GetOrCreateArray(JsonObject json, string propertyName)
    {
        if (json[propertyName] is JsonArray array)
            return array;

        array = [];
        json[propertyName] = array;
        return array;
    }

    public static JsonObject GetOrCreateObject(JsonObject json, string propertyName)
    {
        if (json[propertyName] is JsonObject obj)
            return obj;

        obj = [];
        json[propertyName] = obj;
        return obj;
    }

    public static JsonObject CreateNamePathReference(string name, string path)
    {
        return new JsonObject
        {
            ["name"] = name,
            ["path"] = path
        };
    }

    public static string? GetString(JsonObject json, string propertyName)
    {
        return json[propertyName]?.GetValue<string>();
    }

    public static int GetInt32(JsonObject json, string propertyName, int defaultValue = 0)
    {
        if (json[propertyName] is JsonValue value && value.TryGetValue<int>(out var result))
            return result;

        return defaultValue;
    }
}

