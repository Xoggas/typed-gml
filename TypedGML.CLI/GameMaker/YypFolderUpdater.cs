using System.Text.Json.Nodes;

namespace TypedGML.CLI.GameMaker;

internal sealed class YypFolderUpdater
{
    public string Update(
        string content,
        string originalText,
        IReadOnlySet<string> bclScriptNames,
        IReadOnlyList<FolderEntry> folders,
        string yypFilePath)
    {
        var originalFolders = YypArrayLoader.Load(originalText, "Folders");
        var retainedFolders = originalFolders.Where(ShouldKeepFolder).ToList();
        var generatedFolders = BuildGeneratedFolders(bclScriptNames, folders, yypFilePath)
            .Select(folder => folder.ToJson())
            .Cast<JsonNode?>()
            .ToList();

        if (retainedFolders.Count == originalFolders.Count &&
            generatedFolders.Count == 0)
            return content;

        var updatedFolders = retainedFolders.Concat(generatedFolders).ToList();
        return YypArrayPropertyReplacer.ReplaceOrAdd(
            content,
            "Folders",
            new YypFolderArrayWriter().Write(updatedFolders));
    }

    private static IReadOnlyList<FolderEntry> BuildGeneratedFolders(
        IReadOnlySet<string> bclScriptNames,
        IReadOnlyList<FolderEntry> folders,
        string yypFilePath)
    {
        var generated = folders.ToList();
        if (bclScriptNames.Count > 0)
        {
            var projectName = Path.GetFileNameWithoutExtension(yypFilePath);
            var yypFileName = Path.GetFileName(yypFilePath);
            generated.Add(new FolderEntry("BCL", "folders/BCL.yy", projectName, yypFileName));
        }

        return generated
            .GroupBy(folder => folder.FolderPath, StringComparer.Ordinal)
            .Select(group => group.First())
            .ToList();
    }

    private static bool ShouldKeepFolder(JsonNode? folder)
    {
        var folderPath = folder?["folderPath"]?.GetValue<string>();
        return folderPath is null ||
            (!folderPath.StartsWith("folders/BCL", StringComparison.Ordinal) &&
             !folderPath.StartsWith("folders/TypedGML", StringComparison.Ordinal));
    }
}
