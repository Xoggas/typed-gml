namespace TypedGML.Compiler.Symbols;

public sealed class ScopeStack
{
    private readonly Stack<Dictionary<string, string>> _scopes = new();

    public void Push() => _scopes.Push(new Dictionary<string, string>());

    public void Pop() => _scopes.Pop();

    public void Declare(string name, string typeRef)
    {
        if (_scopes.Count == 0)
            Push();

        _scopes.Peek()[name] = typeRef;
    }

    public bool TryResolve(string name, out string typeRef)
    {
        foreach (var scope in _scopes)
            if (scope.TryGetValue(name, out typeRef!))
                return true;

        typeRef = string.Empty;
        return false;
    }
}
