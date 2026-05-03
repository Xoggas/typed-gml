namespace TypedGML.CLI.GameMaker;

internal static class FolderBuilder
{
    private const string RootName = "TypedGML Generated";
    private const string BclName = "BCL";

    public static IReadOnlyList<FolderEntry> Build(
        IReadOnlyList<string> namespaces,
        string projectName,
        string yypRelativePath)
    {
        var entries = new List<FolderEntry>();
        var knownPaths = new HashSet<string>(StringComparer.Ordinal);
        AddEntry(entries, knownPaths, [RootName], projectName, yypRelativePath);

        foreach (var namespaceName in namespaces.Order(StringComparer.Ordinal))
            AddNamespaceFolders(entries, knownPaths, namespaceName);

        return entries;
    }

    private static void AddNamespaceFolders(
        List<FolderEntry> entries,
        HashSet<string> knownPaths,
        string namespaceName)
    {
        var parts = FolderParts(namespaceName);
        for (var length = 2; length <= parts.Count; length++)
            AddEntry(entries, knownPaths, parts.Take(length).ToList(), parts[length - 2], FolderPath(parts.Take(length - 1)));
    }

    private static IReadOnlyList<string> FolderParts(string namespaceName)
    {
        var trimmed = namespaceName.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return [RootName];

        if (string.Equals(trimmed, "TypedGML", StringComparison.Ordinal) ||
            trimmed.StartsWith("TypedGML.", StringComparison.Ordinal))
            return [RootName, BclName];

        return [RootName, .. trimmed.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
    }

    private static void AddEntry(
        List<FolderEntry> entries,
        HashSet<string> knownPaths,
        IReadOnlyList<string> parts,
        string parentName,
        string parentPath)
    {
        var folderPath = FolderPath(parts);
        if (!knownPaths.Add(folderPath))
            return;

        entries.Add(new FolderEntry(parts[^1], folderPath, parentName, parentPath));
    }

    private static string FolderPath(IEnumerable<string> parts) =>
        $"folders/{string.Join("/", parts)}.yy";
}
