using Newtonsoft.Json;

namespace TypedGML.Transpiler.GameMaker;

public sealed class Metadata
{
    [JsonProperty("IDEVersion")]
    public string IdeVersion { get; set; } = string.Empty;
}