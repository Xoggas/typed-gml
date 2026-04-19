using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking;

/// <summary>
///     Registry of all known types keyed by their GML-qualified name.
///     Assigns unique sequential integer IDs used for __TYPE_* macros.
/// </summary>
public sealed class TypeTable
{
    private readonly Dictionary<(string Name, int Arity), TgmlTypeDecl> _byQualified = new();
    private readonly Dictionary<(string Name, int Arity), TgmlTypeDecl> _bySimple = new();
    private readonly Dictionary<(string Name, int Arity), int> _typeIds = new();
    private readonly List<TgmlTypeDecl> _registered = new();
    private int _nextId = 1;

    /// <summary>All registered types in registration order.</summary>
    public IReadOnlyList<TgmlTypeDecl> All => _registered;

    public void Register(TgmlTypeDecl decl, string qualifiedName)
    {
        var arity = decl.TypeParams.Count;
        _byQualified[(qualifiedName, arity)] = decl;
        _bySimple[(decl.Name, arity)] = decl;
        if (!_typeIds.ContainsKey((qualifiedName, arity)))
        {
            _typeIds[(qualifiedName, arity)] = _nextId++;
        }

        decl.QualifiedName = qualifiedName;
        _registered.Add(decl);
    }

    public bool TryResolve(string name, out TgmlTypeDecl? decl)
    {
        if (TryResolve(name, 0, out decl))
        {
            return true;
        }

        var matches = _registered
            .Where(d => string.Equals(d.QualifiedName, name, StringComparison.Ordinal) ||
                        string.Equals(d.Name, name, StringComparison.Ordinal))
            .Distinct()
            .ToList();
        if (matches.Count == 1)
        {
            decl = matches[0];
            return true;
        }

        decl = null;
        return false;
    }

    public bool TryResolve(string name, int typeArgCount, out TgmlTypeDecl? decl)
    {
        if (_byQualified.TryGetValue((name, typeArgCount), out decl))
            return true;

        if (_bySimple.TryGetValue((name, typeArgCount), out decl))
            return true;

        decl = null;
        return false;
    }

    public int GetTypeId(string qualifiedName)
    {
        if (_typeIds.TryGetValue((qualifiedName, 0), out var exact))
            return exact;

        var candidate = _registered.FirstOrDefault(d => string.Equals(d.QualifiedName, qualifiedName, StringComparison.Ordinal));
        return candidate is null ? -1 : GetTypeId(candidate);
    }

    public int GetTypeId(TgmlTypeDecl decl)
    {
        return decl.QualifiedName is not null && _typeIds.TryGetValue((decl.QualifiedName, decl.TypeParams.Count), out var id) ? id : -1;
    }

    /// <summary>All (qualifiedName, id) pairs in registration order.</summary>
    public IEnumerable<(string Name, int Id)> AllTypeIds()
    {
        return _registered.Select(decl => (decl.QualifiedName ?? decl.Name, GetTypeId(decl)));
    }
}
