using TypedGML.Transpiler.Checking;
using TypedGML.Transpiler.Generation.Decorators;
using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Generation;

/// <summary>
///     Contextual state for a single code generation pass over one type declaration.
/// </summary>
public sealed class GenerationContext
{
    public required TranspileContext TranspileContext { get; init; }
    public required DecoratorRegistry DecoratorRegistry { get; init; }

    /// <summary>The type declaration currently being emitted.</summary>
    public TgmlTypeDecl? CurrentType { get; set; }

    /// <summary>If non-null, references to this name inside a with-body are implicit self.</summary>
    public string? WithAlias { get; set; }

    /// <summary>True while emitting a property getter body.</summary>
    public bool InsideGetter { get; set; }

    /// <summary>True while emitting a property setter body.</summary>
    public bool InsideSetter { get; set; }

    /// <summary>True if the current property is declared with 'global' modifier.</summary>
    public bool IsGlobalProperty { get; set; }

    /// <summary>Name of the current property (used for backing field substitution).</summary>
    public string? CurrentPropertyName { get; set; }

    public TypeTable TypeTable => TranspileContext.TypeTable;

    public bool IsGameObject(TgmlTypeDecl decl)
    {
        return decl is TgmlClassDecl cls && cls.IsGameObject;
    }

    /// <summary>
    ///     Look up whether a named type is a @Object class.
    /// </summary>
    public bool IsGameObjectByName(string typeName)
    {
        return TypeTable.TryResolve(typeName, out var t) && t is TgmlClassDecl c && c.IsGameObject;
    }

    public string GmlObjectName(TgmlClassDecl cls)
    {
        return cls.GmlObjectName ?? cls.Name;
    }

    /// <summary>
    ///     Returns the property with the given name declared on the current type
    ///     <b>or any ancestor type</b> in the inheritance chain.
    ///     This ensures that inherited properties (e.g. <c>Health</c> on a subclass of <c>Entity</c>)
    ///     are correctly redirected to <c>get_Health()</c> / <c>set_Health()</c> calls.
    /// </summary>
    public TgmlPropertyDecl? FindProperty(string name)
    {
        return FindPropertyInHierarchy(CurrentType, name, new HashSet<string>(StringComparer.Ordinal));
    }

    private TgmlPropertyDecl? FindPropertyInHierarchy(TgmlTypeDecl? type, string name, HashSet<string> visited)
    {
        if (type is null) return null;

        var key = type.QualifiedName ?? type.Name;
        if (!visited.Add(key)) return null; // cycle guard

        // Check own properties first
        var own = type switch
        {
            TgmlClassDecl cls => cls.Properties.FirstOrDefault(p => p.Name == name),
            TgmlStructDecl str => str.Properties.FirstOrDefault(p => p.Name == name),
            _ => null
        };
        if (own is not null) return own;

        // Walk base types
        var bases = type switch
        {
            TgmlClassDecl cls => cls.BaseTypes,
            TgmlStructDecl str => str.BaseTypes,
            _ => null
        };
        if (bases is null) return null;

        foreach (var bt in bases)
        {
            TypeTable.TryResolve(bt.Name.Full, out var baseDecl);
            var found = FindPropertyInHierarchy(baseDecl, name, visited);
            if (found is not null) return found;
        }

        return null;
    }

    /// <summary>
    ///     Returns true when we are currently emitting the getter or setter body of the
    ///     named property. Used to prevent infinite recursion when redirecting property
    ///     reads/writes to get_X / set_X calls.
    /// </summary>
    public bool IsInsideAccessorOf(string propertyName)
    {
        return (InsideGetter || InsideSetter) && CurrentPropertyName == propertyName;
    }
}