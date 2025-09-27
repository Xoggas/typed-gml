using Newtonsoft.Json;
using TypedGML.Transpiler.GameMaker.IO;

namespace TypedGML.Transpiler.GameMaker;

public sealed class Folder
{
    [JsonProperty("$GMFolder")]
    public string GameMakerFolder { get; set; } = string.Empty;

    [JsonProperty("%Name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("folderPath")]
    public string FolderPath { get; set; } = string.Empty;

    [JsonProperty("name")]
    public string InternalName { get; set; } = string.Empty;

    [JsonProperty("resourceType")]
    public string ResourceType { get; set; } = "GMFolder";

    [JsonProperty("resourceVersion")]
    public string ResourceVersion { get; set; } = "2.0";

    public static Folder Create(string folderName, string folderPath)
    {
        return new Folder
        {
            Name = folderName,
            FolderPath = PathExtensions.Combine("folders", folderPath) + ".yy",
            InternalName = folderName
        };
    }
}