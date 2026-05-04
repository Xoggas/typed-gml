using System.Text.Json.Nodes;

namespace TypedGML.CLI.GameMaker;

internal record FolderEntry(
    string Name,
    string FolderPath,
    string ParentName,
    string ParentPath)
{
    public JsonObject ToJson() => new()
    {
        ["$GMFolder"] = string.Empty,
        ["%Name"] = Name,
        ["folderPath"] = FolderPath,
        ["name"] = Name,
        ["order"] = 0,
        ["parent"] = new JsonObject
        {
            ["name"] = ParentName,
            ["path"] = ParentPath
        },
        ["resourceType"] = "GMFolder",
        ["resourceVersion"] = "2.0",
        ["tags"] = new JsonArray()
    };
}
