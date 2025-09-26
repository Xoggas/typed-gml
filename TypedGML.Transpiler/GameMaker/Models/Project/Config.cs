using System.Text.Json.Serialization;

namespace TypedGML.Transpiler.GameMaker;

public sealed class Config
{
    [JsonPropertyName("children")]
    public List<object> Children { get; set; } = new();

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}