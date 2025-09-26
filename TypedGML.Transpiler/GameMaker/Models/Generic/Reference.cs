using System.Text.Json.Serialization;

namespace TypedGML.Transpiler.GameMaker;

public sealed class Reference
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;
}