namespace TypedGML.Transpiler.Generation;

public sealed partial class GenerationContext
{
    public void PushIdentifierAliases(IEnumerable<KeyValuePair<string, string>> aliases)
        => _identifierAliasScopes.Push(new Dictionary<string, string>(aliases, StringComparer.Ordinal));

    public void PopIdentifierAliases()
    {
        if (_identifierAliasScopes.Count > 0)
            _identifierAliasScopes.Pop();
    }

    public bool TryGetIdentifierAlias(string name, out string alias)
    {
        foreach (var scope in _identifierAliasScopes)
        {
            if (scope.TryGetValue(name, out var foundAlias))
            {
                alias = foundAlias;
                return true;
            }
        }

        alias = string.Empty;
        return false;
    }

    public void PushLocalScope(IEnumerable<string> names)
        => _localShadowScopes.Push(new HashSet<string>(names, StringComparer.Ordinal));

    public void PopLocalScope()
    {
        if (_localShadowScopes.Count > 0)
            _localShadowScopes.Pop();
    }

    public void DeclareLocal(string name)
    {
        if (_localShadowScopes.Count > 0)
            _localShadowScopes.Peek().Add(name);
    }

    public bool IsLocalShadow(string name)
    {
        foreach (var scope in _localShadowScopes)
            if (scope.Contains(name))
                return true;

        return false;
    }

    public string AllocateTempIdentifier(string prefix)
    {
        _tempCounter++;
        return $"__{prefix}_{_tempCounter}";
    }
}
