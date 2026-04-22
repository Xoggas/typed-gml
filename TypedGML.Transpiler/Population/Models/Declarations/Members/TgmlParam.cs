namespace TypedGML.Transpiler.Population.Models;

/// <summary>A method/constructor/lambda parameter declaration.</summary>
public sealed class TgmlParam
{
    /// <summary>Decorators applied to this parameter (e.g. <c>@NativeProperty</c>).</summary>
    public List<TgmlDecorator> Decorators { get; init; } = new();

    /// <summary>The declared type of this parameter.</summary>
    public required TgmlTypeRef Type { get; init; }

    /// <summary>The parameter name as written in source.</summary>
    public required string Name { get; init; }

    /// <summary>Optional default-value expression; <c>null</c> means the parameter is required.</summary>
    public TgmlExpression? Default { get; init; }
}