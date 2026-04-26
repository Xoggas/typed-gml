using System.Text;

namespace TypedGML.Compiler.Emission;

public sealed class GmlWriter
{
    private readonly StringBuilder _builder = new();
    private int _indentLevel;

    public void Write(string text) => _builder.Append(text);

    public void WriteLine(string line)
    {
        _builder.Append(new string(' ', _indentLevel * 4));
        _builder.AppendLine(line);
    }

    public void BeginBlock()
    {
        _builder.AppendLine(" {");
        Indent();
    }

    public void EndBlock()
    {
        Dedent();
        WriteLine("}");
    }

    public void Indent() => _indentLevel++;

    public void Dedent()
    {
        if (_indentLevel > 0)
            _indentLevel--;
    }

    public string GetOutput() => _builder.ToString();
}
