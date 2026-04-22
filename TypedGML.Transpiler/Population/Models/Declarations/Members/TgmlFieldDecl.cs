namespace TypedGML.Transpiler.Population.Models;

/// <summary>
///     A field declaration inside a class or struct.
///     Static fields are emitted to <c>global.OwnerName_FieldName</c>;
///     instance fields are emitted as <c>self.FieldName = default;</c> in the constructor.
/// </summary>
public sealed class TgmlFieldDecl : TgmlMemberDecl
{
    /// <summary>The declared type of the field.</summary>
    public required TgmlTypeRef Type { get; init; }

    /// <summary><c>true</c> when the <c>static</c> modifier is present.</summary>
    public bool IsStatic { get; init; }

    /// <summary><c>true</c> when the <c>readonly</c> modifier is present.</summary>
    public bool IsReadonly { get; init; }

    /// <summary><c>true</c> when the <c>const</c> modifier is present.</summary>
    public bool IsConst { get; init; }

    /// <summary>Optional initialiser expression; when absent the type's default value is used.</summary>
    public TgmlExpression? Initializer { get; init; }
}