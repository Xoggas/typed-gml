using Newtonsoft.Json;

namespace TypedGML.Transpiler.GameMaker;

public sealed class ResourceEntry
{
    [JsonProperty("id")]
    public Reference Id { get; set; } = new();
}