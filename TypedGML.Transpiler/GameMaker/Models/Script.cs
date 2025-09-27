using Newtonsoft.Json;

namespace TypedGML.Transpiler.GameMaker;

public sealed class Script
{
    [JsonProperty("$GMScript")]
    public string GameMakerScript { get; set; } = "v1";

    [JsonProperty("%Name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("isCompatibility")]
    public bool IsCompatibility { get; set; } = false;

    [JsonProperty("isDnD")]
    public bool IsDnD { get; set; } = false;

    [JsonProperty("name")]
    public string InternalName { get; set; } = string.Empty;

    [JsonProperty("parent")]
    public Reference Parent { get; set; } = new();

    [JsonProperty("resourceType")]
    public string ResourceType { get; set; } = "GMScript";

    [JsonProperty("resourceVersion")]
    public string ResourceVersion { get; set; } = "2.0";
}