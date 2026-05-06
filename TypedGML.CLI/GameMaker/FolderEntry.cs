using System.Text.Json.Nodes;

namespace TypedGML.CLI.GameMaker;

internal record FolderEntry(
    string Name,
    string FolderPath)
{
    public JsonObject ToJson() => new()
    {
        ["$GMFolder"] = string.Empty,
        ["%Name"] = Name,
        ["folderPath"] = FolderPath,
        ["name"] = Name,
        ["resourceType"] = "GMFolder",
        ["resourceVersion"] = "2.0"
    };
}
