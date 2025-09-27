using Newtonsoft.Json;

namespace TypedGML.Transpiler.GameMaker;

public sealed class AudioGroup
{
    [JsonProperty("$GMAudioGroup")]
    public string GameMakerAudioGroup { get; set; } = string.Empty;

    [JsonProperty("%Name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("name")]
    public string InternalName { get; set; } = string.Empty;

    [JsonProperty("resourceType")]
    public string ResourceType { get; set; } = "GMAudioGroup";

    [JsonProperty("resourceVersion")]
    public string ResourceVersion { get; set; } = "2.0";

    [JsonProperty("targets")]
    public int Targets { get; set; }
}