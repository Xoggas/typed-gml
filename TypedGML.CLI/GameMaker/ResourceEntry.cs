using System.Text.Json.Nodes;

namespace TypedGML.CLI.GameMaker;

internal sealed record ResourceEntry(string Name, string Path)
{
    public static ResourceEntry? FromJson(JsonNode? node)
    {
        var name = node?["id"]?["name"]?.GetValue<string>();
        var path = node?["id"]?["path"]?.GetValue<string>();
        return name is null || path is null ? null : new ResourceEntry(name, path);
    }

    public static ResourceEntry Script(string name) =>
        new(name, $"scripts/{name}/{name}.yy");

    public static ResourceEntry Object(string name) =>
        new(name, $"objects/{name}/{name}.yy");

    public JsonObject ToJson() => new()
    {
        ["id"] = new JsonObject
        {
            ["name"] = Name,
            ["path"] = Path
        }
    };
}
