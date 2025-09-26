using System.Text.Json.Serialization;

namespace TypedGML.Transpiler.GameMaker;

public sealed class AudioGroup
{
    [JsonPropertyName("$GMAudioGroup")]
    public string GameMakerAudioGroup { get; set; } = string.Empty;

    [JsonPropertyName("%Name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string InternalName { get; set; } = string.Empty;

    [JsonPropertyName("resourceType")]
    public string ResourceType { get; set; } = "GMAudioGroup";

    [JsonPropertyName("resourceVersion")]
    public string ResourceVersion { get; set; } = "2.0";

    [JsonPropertyName("targets")]
    public int Targets { get; set; }
}