using TypedGML.Compiler.Emission;

namespace TypedGML.CLI;

internal sealed class CollectingGmlOutputSink : IGmlOutputSink
{
    private readonly FileGmlOutputSink? _fileSink;
    private readonly GmlOutputFormatter _formatter = new();
    private readonly Dictionary<string, string> _output = new(StringComparer.OrdinalIgnoreCase);

    public CollectingGmlOutputSink(bool writeFiles)
    {
        _fileSink = writeFiles ? new FileGmlOutputSink() : null;
    }

    public IReadOnlyDictionary<string, string> Output => _output;

    public void Write(string path, string content)
    {
        _output[path] = _formatter.Normalize(content);
        _fileSink?.Write(path, content);
    }
}
