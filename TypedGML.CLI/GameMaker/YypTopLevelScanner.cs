namespace TypedGML.CLI.GameMaker;

internal sealed class YypTopLevelScanner(string text)
{
    private int _index;
    private int _depth;
    private bool _inString;
    private bool _isEscaped;

    public bool TryFindArrayProperty(string propertyName, out YypResourceArraySpan span)
    {
        span = default;
        while (_index < text.Length)
        {
            if (IsTopLevelPropertyStart())
            {
                var key = ReadString();
                if (key == propertyName && TryReadArraySpan(out span))
                    return true;

                continue;
            }

            Advance();
        }

        return false;
    }

    private bool IsTopLevelPropertyStart() =>
        !_inString && _depth == 1 && text[_index] == '"';

    private string ReadString()
    {
        var start = ++_index;
        var escaped = false;
        while (_index < text.Length)
        {
            var current = text[_index];
            if (escaped)
            {
                escaped = false;
                _index++;
                continue;
            }

            if (current == '\\')
            {
                escaped = true;
                _index++;
                continue;
            }

            if (current == '"')
            {
                var value = text[start.._index];
                _index++;
                return value;
            }

            _index++;
        }

        return string.Empty;
    }

    private bool TryReadArraySpan(out YypResourceArraySpan span)
    {
        span = default;
        SkipWhitespace();
        if (!TryRead(':'))
            return false;

        SkipWhitespace();
        if (_index >= text.Length || text[_index] != '[')
            return false;

        var start = _index;
        var end = FindMatchingArrayEnd();
        if (end < 0)
            return false;

        span = new YypResourceArraySpan(start, end);
        _index = end + 1;
        return true;
    }

    private int FindMatchingArrayEnd()
    {
        var arrayDepth = 0;
        var inString = false;
        var escaped = false;

        for (var i = _index; i < text.Length; i++)
        {
            var current = text[i];
            UpdateStringState(current, ref inString, ref escaped);
            if (inString)
                continue;

            if (current == '[')
                arrayDepth++;
            else if (current == ']' && --arrayDepth == 0)
                return i;
        }

        return -1;
    }
    private void SkipWhitespace()
    {
        while (_index < text.Length && char.IsWhiteSpace(text[_index]))
            _index++;
    }

    private bool TryRead(char expected)
    {
        if (_index >= text.Length || text[_index] != expected)
            return false;

        _index++;
        return true;
    }

    private void Advance()
    {
        var current = text[_index++];
        UpdateStringState(current, ref _inString, ref _isEscaped);
        if (_inString)
            return;

        if (current is '{' or '[')
            _depth++;
        else if (current is '}' or ']')
            _depth--;
    }

    private static void UpdateStringState(char current, ref bool inString, ref bool escaped)
    {
        if (escaped)
        {
            escaped = false;
            return;
        }

        if (current == '\\')
            escaped = inString;
        else if (current == '"')
            inString = !inString;
    }
}
