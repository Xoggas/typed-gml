namespace TypedGML.CLI.GameMaker;

internal sealed class GeneratedResourceCleaner
{
    public void DeleteStale(
        string gmRoot,
        GeneratedResourceManifest previous,
        GeneratedResourceManifest current)
    {
        foreach (var name in Stale(previous.Scripts, current.Scripts))
            DeleteResourceDirectory(gmRoot, "scripts", name);

        foreach (var name in Stale(previous.Objects, current.Objects))
            DeleteResourceDirectory(gmRoot, "objects", name);
    }

    private static IEnumerable<string> Stale(IEnumerable<string> previous, IEnumerable<string> current)
    {
        var currentSet = current.ToHashSet(StringComparer.Ordinal);
        return previous.Where(name => !currentSet.Contains(name));
    }

    private static void DeleteResourceDirectory(string gmRoot, string kind, string name)
    {
        if (Path.GetFileName(name) != name)
            return;

        var parent = Path.GetFullPath(Path.Combine(gmRoot, kind));
        var path = Path.GetFullPath(Path.Combine(parent, name));
        if (!IsInside(path, parent) || !Directory.Exists(path))
            return;

        Directory.Delete(path, true);
    }

    private static bool IsInside(string path, string parent)
    {
        var normalizedParent = parent.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        return path.StartsWith(normalizedParent, StringComparison.OrdinalIgnoreCase);
    }
}
