namespace TypedGML.CLI.GameMaker;

internal static class FolderBuilder
{
    public static IReadOnlyList<FolderEntry> Build(
        IEnumerable<string> folderPaths,
        string projectName,
        string yypRelativePath)
    {
        var normalized = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var folderPath in folderPaths)
            AddWithIntermediates(normalized, folderPath);

        return normalized
            .Select(path => BuildEntry(path, projectName, yypRelativePath))
            .ToList();
    }

    public static void AddWithIntermediates(ISet<string> set, string folderPath)
    {
        var parts = GameMakerFolderPath.Normalize(folderPath)
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var current = string.Empty;

        foreach (var part in parts)
        {
            current = current.Length == 0 ? part : $"{current}/{part}";
            set.Add(current);
        }
    }

    private static FolderEntry BuildEntry(
        string folderPath,
        string projectName,
        string yypRelativePath)
    {
        var parent = GameMakerFolderPath.FolderParent(folderPath, projectName, yypRelativePath);
        return new FolderEntry(
            GameMakerFolderPath.Name(folderPath),
            GameMakerFolderPath.ResourcePath(folderPath),
            parent.Name,
            parent.Path);
    }
}
