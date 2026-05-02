namespace TypedGML.Compiler.Emission;

public sealed class EmissionNarrowingState
{
    private readonly Dictionary<string, string> _types = new(StringComparer.Ordinal);

    public bool TryResolve(string name, out string typeRef) =>
        _types.TryGetValue(name, out typeRef!);

    public void Narrow(string name, string typeRef) =>
        _types[name] = typeRef;

    public void Clear(string name) =>
        _types.Remove(name);

    public void WithScope(Action action)
    {
        var snapshot = new Dictionary<string, string>(_types, StringComparer.Ordinal);
        try
        {
            action();
        }
        finally
        {
            _types.Clear();
            foreach (var entry in snapshot)
                _types[entry.Key] = entry.Value;
        }
    }
}
