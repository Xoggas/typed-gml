using Newtonsoft.Json;

namespace TypedGML.Transpiler.GameMaker;

public sealed class TextureGroup
{
    [JsonProperty("$GMTextureGroup")]
    public string GameMakerTextureGroup { get; set; } = string.Empty;

    [JsonProperty("%Name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("autocrop")]
    public bool AutoCrop { get; set; }

    [JsonProperty("border")]
    public int Border { get; set; }

    [JsonProperty("compressFormat")]
    public string CompressFormat { get; set; } = string.Empty;

    [JsonProperty("customOptions")]
    public string CustomOptions { get; set; } = string.Empty;

    [JsonProperty("directory")]
    public string Directory { get; set; } = string.Empty;

    [JsonProperty("groupParent")]
    public Reference? GroupParent { get; set; }

    [JsonProperty("isScaled")]
    public bool IsScaled { get; set; }

    [JsonProperty("loadType")]
    public string LoadType { get; set; } = string.Empty;

    [JsonProperty("mipsToGenerate")]
    public int MipsToGenerate { get; set; }

    [JsonProperty("name")]
    public string InternalName { get; set; } = string.Empty;

    [JsonProperty("resourceType")]
    public string ResourceType { get; set; } = "GMTextureGroup";

    [JsonProperty("resourceVersion")]
    public string ResourceVersion { get; set; } = "2.0";

    [JsonProperty("targets")]
    public int Targets { get; set; }
}