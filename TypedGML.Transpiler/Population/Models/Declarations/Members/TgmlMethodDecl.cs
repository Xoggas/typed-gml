namespace TypedGML.Transpiler.Population.Models;

/// <summary>
///     A method declaration inside a class, struct or interface.
///     Includes operator overloads and user-defined conversion operators.
/// </summary>
public sealed class TgmlMethodDecl : TgmlMemberDecl
{
    /// <summary>The declared return type.</summary>
    public required TgmlTypeRef ReturnType { get; init; }

    /// <summary>Visibility, static, virtual/override/abstract and nocheck modifiers.</summary>
    public required MethodModifiers Modifiers { get; init; }

    /// <summary>Generic type parameters declared on this method (e.g. <c>&lt;T&gt;</c>).</summary>
    public List<TgmlTypeParam> TypeParams { get; init; } = new();

    /// <summary>Ordered list of formal parameters.</summary>
    public List<TgmlParam> Params { get; init; } = new();

    /// <summary>
    ///     For operator overloads the token string (<c>"+"</c>, <c>"=="</c>, etc.);
    ///     <c>null</c> for regular methods and conversion operators.
    /// </summary>
    public string? OperatorToken { get; init; }

    /// <summary>
    ///     For user-defined conversion operators, whether the conversion is
    ///     <c>implicit</c> or <c>explicit</c>; <see cref="ConversionModifier.None"/> otherwise.
    /// </summary>
    public ConversionModifier Conversion { get; init; }

    /// <summary>
    ///     Method body, or <c>null</c> for abstract methods and interface method stubs.
    /// </summary>
    public TgmlBlock? Body { get; init; }

    // ── Computed properties ──────────────────────────────────────────────

    /// <summary><c>true</c> when the method is <c>abstract</c> (no body).</summary>
    public bool IsAbstract  => Modifiers.Virtual == VirtualModifier.Abstract;

    /// <summary><c>true</c> when the method is marked <c>virtual</c>.</summary>
    public bool IsVirtual   => Modifiers.Virtual == VirtualModifier.Virtual;

    /// <summary><c>true</c> when the method is marked <c>override</c>.</summary>
    public bool IsOverride  => Modifiers.Virtual == VirtualModifier.Override;

    /// <summary><c>true</c> when the method is <c>static</c>.</summary>
    public bool IsStatic    => Modifiers.IsStatic;

    /// <summary><c>true</c> when this method is a binary/unary operator overload.</summary>
    public bool IsOperatorOverload => OperatorToken is not null;

    /// <summary><c>true</c> when this method is an implicit or explicit conversion operator.</summary>
    public bool IsConversionOperator => Conversion != ConversionModifier.None;

    /// <summary><c>true</c> when this is any kind of user-defined operator (overload or conversion).</summary>
    public bool IsUserDefinedOperator => IsOperatorOverload || IsConversionOperator;
}
