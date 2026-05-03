using System.Text;

namespace TypedGML.Compiler.Emission;

public sealed class GmlWriter
{
    private readonly StringBuilder _builder = new();
    private int _indentLevel;
    private bool _atLineStart = true;

    public void Write(string text)
    {
        if (_atLineStart && _indentLevel > 0)
            _builder.Append(new string(' ', _indentLevel * 4));
        _builder.Append(text);
        _atLineStart = false;
    }

    public void WriteLine(string line)
    {
        _builder.Append(new string(' ', _indentLevel * 4));
        _builder.AppendLine(line);
        _atLineStart = true;
    }

    public void BeginBlock()
    {
        _builder.AppendLine(" {");
        Indent();
        _atLineStart = true;
    }

    public void EndBlock()
    {
        Dedent();
        _builder.Append(new string(' ', _indentLevel * 4));
        _builder.AppendLine("}");
        _atLineStart = true;
    }

    public void Indent() => _indentLevel++;

    public void Dedent()
    {
        if (_indentLevel > 0)
            _indentLevel--;
    }

    public string GetOutput() => _builder.ToString();
}
