namespace TypedGML.Transpiler.Population.Models;

/// <summary>A type reference, e.g. "List&lt;string&gt;[]" or "number".</summary>
public sealed class TgmlTypeRef
{
    public required TgmlQualifiedName Name { get; init; }
    public List<TgmlTypeRef> TypeArgs { get; init; } = new();

    /// <summary>Number of [] array dimensions.</summary>
    public int ArrayDepth { get; init; }

    /// <summary>GML name of the base type (no array brackets, no type args).</summary>
    public string GmlBaseName => Name.Gml;

    public bool IsArray => ArrayDepth > 0;

    public override string ToString()
    {
        var typeArgs = TypeArgs.Count > 0
            ? $"<{string.Join(", ", TypeArgs)}>"
            : string.Empty;
        var arr = string.Concat(Enumerable.Repeat("[]", ArrayDepth));
        return $"{Name.Full}{typeArgs}{arr}";
    }
}
