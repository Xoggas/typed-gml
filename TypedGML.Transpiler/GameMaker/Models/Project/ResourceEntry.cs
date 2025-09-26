using System.Text.Json.Serialization;

namespace TypedGML.Transpiler.GameMaker;

public sealed class ResourceEntry
{
    [JsonPropertyName("id")]
    public Reference Id { get; set; } = new();
}