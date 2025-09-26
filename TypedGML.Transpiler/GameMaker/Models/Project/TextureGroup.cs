using System.Text.Json.Serialization;

namespace TypedGML.Transpiler.GameMaker;

public sealed class TextureGroup
{
    [JsonPropertyName("$GMTextureGroup")]
    public string GameMakerTextureGroup { get; set; } = string.Empty;

    [JsonPropertyName("%Name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("autocrop")]
    public bool AutoCrop { get; set; }

    [JsonPropertyName("border")]
    public int Border { get; set; }

    [JsonPropertyName("compressFormat")]
    public string CompressFormat { get; set; } = string.Empty;

    [JsonPropertyName("customOptions")]
    public string CustomOptions { get; set; } = string.Empty;

    [JsonPropertyName("directory")]
    public string Directory { get; set; } = string.Empty;

    [JsonPropertyName("groupParent")]
    public Reference? GroupParent { get; set; }

    [JsonPropertyName("isScaled")]
    public bool IsScaled { get; set; }

    [JsonPropertyName("loadType")]
    public string LoadType { get; set; } = string.Empty;

    [JsonPropertyName("mipsToGenerate")]
    public int MipsToGenerate { get; set; }

    [JsonPropertyName("name")]
    public string InternalName { get; set; } = string.Empty;

    [JsonPropertyName("resourceType")]
    public string ResourceType { get; set; } = "GMTextureGroup";

    [JsonPropertyName("resourceVersion")]
    public string ResourceVersion { get; set; } = "2.0";

    [JsonPropertyName("targets")]
    public int Targets { get; set; }
}