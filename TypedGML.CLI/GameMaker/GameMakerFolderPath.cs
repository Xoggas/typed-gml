namespace TypedGML.CLI.GameMaker;

internal static class GameMakerFolderPath
{
    public const string Bcl = "BCL";

    public static string? FromSource(string sourceFilePath, string tgmlRoot)
    {
        var rel = Path.GetRelativePath(tgmlRoot, sourceFilePath);
        var dir = Path.GetDirectoryName(rel);
        return string.IsNullOrEmpty(dir)
            ? null
            : dir.Replace('\\', '/');
    }

    public static string ResourcePath(string folderPath) =>
        $"folders/{folderPath}.yy";

    public static string Name(string folderPath)
    {
        var normalized = Normalize(folderPath);
        var index = normalized.LastIndexOf('/');
        return index < 0 ? normalized : normalized[(index + 1)..];
    }

    public static (string Name, string Path) AssetParent(
        string? folderPath,
        string projectName,
        string yypFileName) =>
        string.IsNullOrEmpty(folderPath)
            ? (projectName, yypFileName)
            : (Name(folderPath), ResourcePath(folderPath));

    public static (string Name, string Path) FolderParent(
        string folderPath,
        string projectName,
        string yypFileName)
    {
        var parent = Parent(folderPath);
        return parent is null
            ? (projectName, yypFileName)
            : (Name(parent), ResourcePath(parent));
    }

    public static string Normalize(string folderPath) =>
        folderPath.Replace('\\', '/').Trim('/');

    private static string? Parent(string folderPath)
    {
        var normalized = Normalize(folderPath);
        var index = normalized.LastIndexOf('/');
        return index < 0 ? null : normalized[..index];
    }
}
