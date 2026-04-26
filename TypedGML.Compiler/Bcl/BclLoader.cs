using System.IO;

namespace TypedGML.Compiler.Bcl;

public sealed class BclLoader(string bclDirectory)
{
    public IReadOnlyList<string> GetFiles() =>
        Directory.Exists(bclDirectory)
            ? Directory.GetFiles(bclDirectory, "*.tgml", SearchOption.AllDirectories)
            : [];
}
