using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking;

/// <summary>
///     Immutable snapshot of the position inside a callable body walk.
///     Passed to every hook in <see cref="AstBodyWalker" />.
/// </summary>
public sealed class WalkContext
{
    /// <summary>The type declaration that owns the body being walked.</summary>
    public required TgmlTypeDecl OwnerType { get; init; }

    /// <summary>
    ///     The member whose body is being walked (method, property, constructor).
    ///     <c>null</c> when walking a field initializer.
    /// </summary>
    public TgmlMemberDecl? Member { get; init; }

    /// <summary>True when walking inside a constructor body.</summary>
    public bool InConstructor { get; init; }

    /// <summary>
    ///     Declared return type name of the current callable.
    ///     <c>"void"</c> for constructors, setters, and void methods.
    /// </summary>
    public string ReturnTypeName { get; init; } = "void";

    /// <summary>Parameters of the current callable (empty for field contexts).</summary>
    public IReadOnlyList<TgmlParam> Params { get; init; } = [];
}

