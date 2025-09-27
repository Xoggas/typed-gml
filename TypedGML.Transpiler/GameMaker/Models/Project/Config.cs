using Newtonsoft.Json;

namespace TypedGML.Transpiler.GameMaker;

public sealed class Config
{
    [JsonProperty("children")]
    public List<object> Children { get; set; } = new();

    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;
}