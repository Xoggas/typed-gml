using System.IO;

namespace TypedGML.Compiler.Emission;

public sealed class FileGmlOutputSink(GmlOutputFormatter formatter) : IGmlOutputSink
{
    public FileGmlOutputSink()
        : this(new GmlOutputFormatter())
    {
    }

    public void Write(string path, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, formatter.Normalize(content));
    }
}
