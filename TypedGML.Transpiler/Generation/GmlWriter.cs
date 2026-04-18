using System.Text;

namespace TypedGML.Transpiler.Generation;

/// <summary>
///     A streaming GML code writer with indentation support.
/// </summary>
public sealed class GmlWriter
{
    private readonly StringBuilder _sb = new();
    private int _indent;
    private bool _newLine = true;

    public void Indent()
    {
        _indent++;
    }

    public void Dedent()
    {
        _indent = Math.Max(0, _indent - 1);
    }

    public void Write(string text)
    {
        if (_newLine && text.Length > 0)
        {
            _sb.Append(new string(' ', _indent * 4));
            _newLine = false;
        }

        _sb.Append(text);
    }

    public void WriteLine(string text = "")
    {
        if (text.Length > 0)
        {
            if (_newLine)
            {
                _sb.Append(new string(' ', _indent * 4));
            }

            _sb.Append(text);
        }

        _sb.AppendLine();
        _newLine = true;
    }

    public void WriteLineRaw(string text)
    {
        _sb.AppendLine(text);
        _newLine = true;
    }

    public void OpenBrace()
    {
        WriteLine("{");
        Indent();
    }

    public void CloseBrace()
    {
        Dedent();
        WriteLine("}");
    }

    public override string ToString()
    {
        return _sb.ToString();
    }
}