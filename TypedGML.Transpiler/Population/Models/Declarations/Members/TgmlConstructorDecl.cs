namespace TypedGML.Transpiler.Population.Models;

public sealed class TgmlConstructorDecl : TgmlMemberDecl
{
    public List<TgmlParam> Params { get; init; } = new();

    /// <summary>Arguments passed to base(...), null if no base call.</summary>
    public List<TgmlExpression>? BaseArgs { get; init; }

    public required TgmlBlock Body { get; init; }
    public bool HasBaseCall => BaseArgs is not null;
}