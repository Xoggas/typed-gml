using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace TypedGML.Transpiler.GameMaker;

public sealed class Project
{
    [JsonProperty("$GMProject")]
    public string GameMakerProject { get; set; } = "v1";

    [JsonProperty("%Name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("AudioGroups")]
    public List<AudioGroup> AudioGroups { get; set; } = [];

    [JsonProperty("configs")]
    public Config Configs { get; set; } = new();

    [JsonProperty("defaultScriptType")]
    public int DefaultScriptType { get; set; }

    [JsonProperty("Folders")]
    public List<Folder> Folders { get; set; } = [];

    [JsonProperty("ForcedPrefabProjectReferences")]
    public List<object> ForcedPrefabProjectReferences { get; set; } = [];

    [JsonProperty("IncludedFiles")]
    public List<object> IncludedFiles { get; set; } = [];

    [JsonProperty("isEcma")]
    public bool IsEcma { get; set; }

    [JsonProperty("LibraryEmitters")]
    public List<object> LibraryEmitters { get; set; } = [];

    [JsonProperty("MetaData")]
    public Metadata MetaData { get; set; } = new();

    [JsonProperty("name")]
    public string InternalName { get; set; } = string.Empty;

    [JsonProperty("resources")]
    public List<ResourceEntry> Resources { get; set; } = [];

    [JsonProperty("resourceType")]
    public string ResourceType { get; set; } = "GMProject";

    [JsonProperty("resourceVersion")]
    public string ResourceVersion { get; set; } = "2.0";

    [JsonProperty("RoomOrderNodes")]
    public List<RoomOrderNode> RoomOrderNodes { get; set; } = [];

    [JsonProperty("templateType")]
    public string TemplateType { get; set; } = string.Empty;

    [JsonProperty("TextureGroups")]
    public List<TextureGroup> TextureGroups { get; set; } = [];
}