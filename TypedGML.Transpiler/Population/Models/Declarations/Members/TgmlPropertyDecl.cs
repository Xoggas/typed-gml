namespace TypedGML.Transpiler.Population.Models;

/// <summary>A single get or set accessor declaration.</summary>
public sealed class TgmlAccessorDecl
{
    public bool IsGet { get; init; }
    public bool IsSet => !IsGet;
    public AccessModifier? AccessMod { get; init; } // null = inherits property access

    /// <summary>null means auto (empty body = ; accessor).</summary>
    public TgmlBlock? Body { get; init; }

    public bool IsAuto => Body is null;
}

public sealed class TgmlPropertyDecl : TgmlMemberDecl
{
    public required TgmlTypeRef Type { get; init; }
    public required PropertyModifiers Modifiers { get; init; }
    public List<TgmlAccessorDecl> Accessors { get; init; } = new();

    public TgmlAccessorDecl? Getter => Accessors.FirstOrDefault(a => a.IsGet);
    public TgmlAccessorDecl? Setter => Accessors.FirstOrDefault(a => a.IsSet);
    public bool IsGlobal => Modifiers.Scope == ScopeModifier.Global;
    public bool IsStatic => Modifiers.Scope == ScopeModifier.Static;
}