using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking;

/// <summary>
///     Registry of all known types keyed by their GML-qualified name.
///     Assigns unique sequential integer IDs used for __TYPE_* macros.
/// </summary>
public sealed class TypeTable
{
    private readonly Dictionary<string, TgmlTypeDecl> _byQualified = new(StringComparer.Ordinal);
    private readonly Dictionary<string, TgmlTypeDecl> _bySimple = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _typeIds = new(StringComparer.Ordinal);
    private int _nextId = 1;

    /// <summary>All registered types in registration order.</summary>
    public IReadOnlyDictionary<string, TgmlTypeDecl> All => _byQualified;

    public void Register(TgmlTypeDecl decl, string qualifiedName)
    {
        _byQualified[qualifiedName] = decl;
        _bySimple.TryAdd(decl.Name, decl);
        if (!_typeIds.ContainsKey(qualifiedName))
        {
            _typeIds[qualifiedName] = _nextId++;
        }

        decl.QualifiedName = qualifiedName;
    }

    public bool TryResolve(string name, out TgmlTypeDecl? decl)
    {
        if (_byQualified.TryGetValue(name, out decl))
        {
            return true;
        }

        if (_bySimple.TryGetValue(name, out decl))
        {
            return true;
        }

        decl = null;
        return false;
    }

    public int GetTypeId(string qualifiedName)
    {
        return _typeIds.GetValueOrDefault(qualifiedName, -1);
    }

    public int GetTypeId(TgmlTypeDecl decl)
    {
        return decl.QualifiedName is not null && _typeIds.TryGetValue(decl.QualifiedName, out var id) ? id : -1;
    }

    /// <summary>All (qualifiedName, id) pairs in registration order.</summary>
    public IEnumerable<(string Name, int Id)> AllTypeIds()
    {
        return _typeIds.Select(kv => (kv.Key, kv.Value));
    }
}