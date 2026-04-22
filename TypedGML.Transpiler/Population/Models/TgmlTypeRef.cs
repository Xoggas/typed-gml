namespace TypedGML.Transpiler.Population.Models;

/// <summary>
///     A reference to a type, optionally generic and/or array-typed.
///     Examples: <c>number</c>, <c>List&lt;string&gt;</c>, <c>Enemy[]</c>.
/// </summary>
public sealed class TgmlTypeRef
{
    /// <summary>The base type name (without type arguments or array brackets).</summary>
    public required TgmlQualifiedName Name { get; init; }

    /// <summary>Generic type arguments supplied to the base type.</summary>
    public List<TgmlTypeRef> TypeArgs { get; init; } = new();

    /// <summary>Number of array dimensions (<c>[]</c> brackets); 0 means not an array.</summary>
    public int ArrayDepth { get; init; }

    /// <summary>GML identifier for the base type (dots replaced with underscores, no brackets).</summary>
    public string GmlBaseName => Name.Gml;

    /// <summary><c>true</c> when this type reference includes at least one array dimension.</summary>
    public bool IsArray => ArrayDepth > 0;

    /// <inheritdoc/>
    public override string ToString()
    {
        var typeArgs = TypeArgs.Count > 0
            ? $"<{string.Join(", ", TypeArgs)}>"
            : string.Empty;
        var arr = string.Concat(Enumerable.Repeat("[]", ArrayDepth));
        return $"{Name.Full}{typeArgs}{arr}";
    }
}
