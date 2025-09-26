using System.Text.Json.Serialization;

namespace TypedGML.Transpiler.GameMaker;

public sealed class Script
{
    [JsonPropertyName("$GMScript")]
    public string GameMakerScript { get; set; } = "v1";

    [JsonPropertyName("%Name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("isCompatibility")]
    public bool IsCompatibility { get; set; } = false;

    [JsonPropertyName("isDnD")]
    public bool IsDnD { get; set; } = false;

    [JsonPropertyName("name")]
    public string InternalName { get; set; } = string.Empty;

    [JsonPropertyName("parent")]
    public Reference Parent { get; set; } = new();

    [JsonPropertyName("resourceType")]
    public string ResourceType { get; set; } = "GMScript";

    [JsonPropertyName("resourceVersion")]
    public string ResourceVersion { get; set; } = "2.0";
}