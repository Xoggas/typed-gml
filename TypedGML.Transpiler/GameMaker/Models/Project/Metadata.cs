using System.Text.Json.Serialization;

namespace TypedGML.Transpiler.GameMaker;

public sealed class Metadata
{
    [JsonPropertyName("IDEVersion")]
    public string IdeVersion { get; set; } = string.Empty;
}