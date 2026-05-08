using System.Text.Json.Serialization;

namespace TypedGML.CLI.GameMaker;

internal sealed class GeneratedResourceManifest
{
    public static GeneratedResourceManifest CreateEmpty() => new();

    [JsonPropertyName("scripts")]
    public List<string> Scripts { get; set; } = [];

    [JsonPropertyName("objects")]
    public List<string> Objects { get; set; } = [];

    [JsonPropertyName("folders")]
    public List<string> Folders { get; set; } = [];

    public static GeneratedResourceManifest From(
        CliCompileResult compileResult,
        IReadOnlyList<FolderEntry> folders)
    {
        return new GeneratedResourceManifest
        {
            Scripts = Sorted(compileResult.Scripts.Keys),
            Objects = Sorted(compileResult.Objects.Select(obj => obj.Name)),
            Folders = Sorted(folders.Select(folder => folder.FolderPath)
                .Concat(compileResult.BclScriptNames.Count > 0 ? [GameMakerFolderPath.ResourcePath(GameMakerFolderPath.Bcl)] : []))
        };
    }

    private static List<string> Sorted(IEnumerable<string> values) =>
        values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();
}
