using Newtonsoft.Json;

namespace TypedGML.Transpiler.GameMaker;

public sealed class Reference
{
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("path")]
    public string Path { get; set; } = string.Empty;

    public static Reference FromFolder(Folder folder)
    {
        return new Reference
        {
            Name = folder.Name,
            Path = folder.FolderPath
        };
    }
}