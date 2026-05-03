using System.IO;

namespace TypedGML.Compiler.Utils;

public sealed class FileScanner
{
    public IReadOnlyList<string> Scan(string rootPath)
    {
        if (!Directory.Exists(rootPath))
            throw new DirectoryNotFoundException($"Input path '{rootPath}' does not exist.");

        return Directory.GetFiles(rootPath, "*.tgml", SearchOption.AllDirectories);
    }
}
