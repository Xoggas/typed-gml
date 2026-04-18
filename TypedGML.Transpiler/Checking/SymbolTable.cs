using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking;

/// <summary>
///     Simple scoped symbol table for tracking local variables and parameters.
/// </summary>
public sealed class SymbolTable
{
    private readonly Stack<Dictionary<string, TgmlTypeRef>> _scopes = new();

    public void PushScope()
    {
        _scopes.Push(new Dictionary<string, TgmlTypeRef>());
    }

    public void PopScope()
    {
        if (_scopes.Count > 0)
        {
            _scopes.Pop();
        }
    }

    public void Define(string name, TgmlTypeRef type)
    {
        if (_scopes.Count == 0)
        {
            PushScope();
        }

        _scopes.Peek()[name] = type;
    }

    public bool TryResolve(string name, out TgmlTypeRef? type)
    {
        foreach (var scope in _scopes)
        {
            if (scope.TryGetValue(name, out type))
            {
                return true;
            }
        }

        type = null;
        return false;
    }
}