using TypedGML.Compiler.Emission;

namespace TypedGML.Compiler.Tests.Helpers;

internal sealed class InMemoryGmlOutputSink : IGmlOutputSink
{
    private readonly GmlOutputFormatter _formatter = new();
    private readonly Dictionary<string, string> _output = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, string> Output => _output;

    public void Write(string path, string content) =>
        _output[path] = _formatter.Normalize(content);
}
