namespace TypedGML.Transpiler.Population.Models;

public sealed class TgmlMethodDecl : TgmlMemberDecl
{
    public required TgmlTypeRef ReturnType { get; init; }
    public required MethodModifiers Modifiers { get; init; }
    public bool IsNocheck { get; init; }
    public List<TgmlTypeParam> TypeParams { get; init; } = new();
    public List<TgmlParam> Params { get; init; } = new();

    /// <summary>null for abstract methods or interface method stubs.</summary>
    public TgmlBlock? Body { get; init; }

    public bool IsAbstract => Modifiers.Virtual == VirtualModifier.Abstract;
    public bool IsVirtual => Modifiers.Virtual == VirtualModifier.Virtual;
    public bool IsOverride => Modifiers.Virtual == VirtualModifier.Override;
    public bool IsStatic => Modifiers.IsStatic;
}