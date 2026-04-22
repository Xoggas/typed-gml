namespace TypedGML.Transpiler.Population.Models;

/// <summary>
///     A constructor declaration on a class or struct.
///     A type may declare multiple constructors (overloads); the emitter dispatches
///     between them at runtime via <c>argument_count</c>.
/// </summary>
public sealed class TgmlConstructorDecl : TgmlMemberDecl
{
    /// <summary>Ordered list of formal parameters.</summary>
    public List<TgmlParam> Params { get; init; } = new();

    /// <summary>
    ///     Arguments passed to <c>base(...)</c>, or <c>null</c> when no base call is present.
    ///     The checker normalises these and stores the resolved expressions in
    ///     <c>Metadata["NormalizedBaseArgs"]</c>.
    /// </summary>
    public List<TgmlArgument>? BaseArgs { get; init; }

    /// <summary>The constructor body (always present; constructors cannot be abstract).</summary>
    public required TgmlBlock Body { get; init; }

    /// <summary><c>true</c> when this constructor explicitly calls <c>base(...)</c>.</summary>
    public bool HasBaseCall => BaseArgs is not null;
}
