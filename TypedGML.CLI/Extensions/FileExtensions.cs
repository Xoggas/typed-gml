namespace TypedGML.CLI.Extensions;

public static class FileExtensions
{
    public static void CopyFilesRecursively(string sourcePath, string targetPath)
    {
        Directory.CreateDirectory(targetPath);

        foreach (var sourceDirectory in Directory.EnumerateDirectories(sourcePath, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourcePath, sourceDirectory);
            Directory.CreateDirectory(Path.Combine(targetPath, relativePath));
        }

        foreach (var sourceFile in Directory.EnumerateFiles(sourcePath, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourcePath, sourceFile);
            var targetFile = Path.Combine(targetPath, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(targetFile) ?? targetPath);
            File.Copy(sourceFile, targetFile, true);
        }
    }
}
