namespace TypedGML.Transpiler.Population.Models;

/// <summary>
///     A delegate type declaration.
///     Delegates compile to a GML struct holding a single callable and satisfy the
///     <c>__types</c> pattern for <c>is</c>/<c>as</c> checks.
/// </summary>
public sealed class TgmlDelegateDecl : TgmlTypeDecl
{
    /// <summary>The declared return type of the delegate's invoke signature.</summary>
    public required TgmlTypeRef ReturnType { get; init; }

    /// <summary>Formal parameters of the delegate's invoke signature.</summary>
    public List<TgmlParam> Params { get; init; } = new();
}
