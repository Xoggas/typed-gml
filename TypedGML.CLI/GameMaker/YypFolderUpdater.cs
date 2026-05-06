using System.Text.Json.Nodes;

namespace TypedGML.CLI.GameMaker;

internal sealed class YypFolderUpdater
{
    public string Update(
        string content,
        string originalText,
        IReadOnlySet<string> bclScriptNames,
        IReadOnlyList<FolderEntry> folders)
    {
        var originalFolders = YypArrayLoader.Load(originalText, "Folders");
        var generatedFolders = BuildGeneratedFolders(bclScriptNames, folders)
            .ToList();
        var currentFolderPaths = generatedFolders
            .Select(folder => folder.FolderPath)
            .ToHashSet(StringComparer.Ordinal);
        var retainedFolders = originalFolders
            .Where(folder => ShouldKeepFolder(folder, currentFolderPaths))
            .ToList();
        var generatedJson = generatedFolders
            .Select(folder => folder.ToJson())
            .Cast<JsonNode?>()
            .ToList();

        if (retainedFolders.Count == originalFolders.Count &&
            generatedJson.Count == 0 &&
            !originalFolders.Any(IsGameMakerFolder))
            return content;

        var updatedFolders = retainedFolders.Concat(generatedJson).ToList();
        return YypArrayPropertyReplacer.ReplaceOrAdd(
            content,
            "Folders",
            new YypFolderArrayWriter().Write(updatedFolders));
    }

    private static IReadOnlyList<FolderEntry> BuildGeneratedFolders(
        IReadOnlySet<string> bclScriptNames,
        IReadOnlyList<FolderEntry> folders)
    {
        var generated = folders.ToList();
        if (bclScriptNames.Count > 0)
            generated.Add(new FolderEntry("BCL", "folders/BCL.yy"));

        return generated
            .GroupBy(folder => folder.FolderPath, StringComparer.Ordinal)
            .Select(group => group.First())
            .ToList();
    }

    private static bool ShouldKeepFolder(JsonNode? folder, IReadOnlySet<string> currentFolderPaths)
    {
        var folderPath = folder?["folderPath"]?.GetValue<string>();
        return folderPath is null || !currentFolderPaths.Contains(folderPath);
    }

    private static bool IsGameMakerFolder(JsonNode? folder) =>
        folder is JsonObject obj &&
        obj.ContainsKey("$GMFolder") &&
        obj.ContainsKey("%Name");
}
