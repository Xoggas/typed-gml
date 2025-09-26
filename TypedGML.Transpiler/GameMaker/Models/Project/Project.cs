using System.Text.Json.Serialization;

namespace TypedGML.Transpiler.GameMaker;

public sealed class Project
{
    [JsonPropertyName("$GMProject")]
    public string GameMakerProject { get; set; } = "v1";

    [JsonPropertyName("%Name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("AudioGroups")]
    public List<AudioGroup> AudioGroups { get; set; } = [];

    [JsonPropertyName("configs")]
    public Config Configs { get; set; } = new();

    [JsonPropertyName("defaultScriptType")]
    public int DefaultScriptType { get; set; }

    [JsonPropertyName("Folders")]
    public List<Folder> Folders { get; set; } = [];

    [JsonPropertyName("ForcedPrefabProjectReferences")]
    public List<object> ForcedPrefabProjectReferences { get; set; } = [];

    [JsonPropertyName("IncludedFiles")]
    public List<object> IncludedFiles { get; set; } = [];

    [JsonPropertyName("isEcma")]
    public bool IsEcma { get; set; }

    [JsonPropertyName("LibraryEmitters")]
    public List<object> LibraryEmitters { get; set; } = [];

    [JsonPropertyName("MetaData")]
    public Metadata MetaData { get; set; } = new();

    [JsonPropertyName("name")]
    public string InternalName { get; set; } = string.Empty;

    [JsonPropertyName("resources")]
    public List<ResourceEntry> Resources { get; set; } = [];

    [JsonPropertyName("resourceType")]
    public string ResourceType { get; set; } = "GMProject";

    [JsonPropertyName("resourceVersion")]
    public string ResourceVersion { get; set; } = "2.0";

    [JsonPropertyName("RoomOrderNodes")]
    public List<RoomOrderNode> RoomOrderNodes { get; set; } = [];

    [JsonPropertyName("templateType")]
    public string TemplateType { get; set; } = string.Empty;

    [JsonPropertyName("TextureGroups")]
    public List<TextureGroup> TextureGroups { get; set; } = [];
}