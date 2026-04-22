namespace TypedGML.Transpiler.Population.Models;

/// <summary>A single member of an enum declaration.</summary>
public sealed class TgmlEnumMember
{
    /// <summary>Decorators applied to this member.</summary>
    public List<TgmlDecorator> Decorators { get; init; } = new();

    /// <summary>The member name as written in source.</summary>
    public required string Name { get; init; }

    /// <summary>
    ///     Explicit integer value expression, or <c>null</c> for auto-increment
    ///     (value = previous member + 1, starting from 0).
    /// </summary>
    public TgmlIntLiteralExpr? Value { get; init; }
}

/// <summary>
///     An enum declaration.  Enums compile to a GML struct of <c>number</c> constants
///     and a corresponding <c>#macro __TYPE_EnumName</c>.
/// </summary>
public sealed class TgmlEnumDecl : TgmlTypeDecl
{
    /// <summary>Enum members in declaration order.</summary>
    public List<TgmlEnumMember> Members { get; init; } = new();
}