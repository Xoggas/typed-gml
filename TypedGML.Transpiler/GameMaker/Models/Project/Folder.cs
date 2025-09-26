using System.Text.Json.Serialization;

namespace TypedGML.Transpiler.GameMaker;

public sealed class Folder
{
    [JsonPropertyName("$GMFolder")]
    public string GameMakerFolder { get; set; } = string.Empty;

    [JsonPropertyName("%Name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("folderPath")]
    public string FolderPath { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string InternalName { get; set; } = string.Empty;

    [JsonPropertyName("resourceType")]
    public string ResourceType { get; set; } = "GMFolder";

    [JsonPropertyName("resourceVersion")]
    public string ResourceVersion { get; set; } = "2.0";
}