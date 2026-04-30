using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Members;

namespace TypedGML.Compiler.Symbols;

public sealed class MemberSymbol
{
    public string Name { get; set; } = string.Empty;

    public MemberKind Kind { get; set; }

    public string ReturnType { get; set; } = string.Empty;

    public IReadOnlyList<ParameterSymbol> Parameters { get; set; } = [];

    public IReadOnlyList<GenericParamNode> GenericParameters { get; set; } = [];

    public IReadOnlySet<string> Modifiers { get; set; } = new HashSet<string>();

    public object? ConstValue { get; set; }

    public ConstructorChainTarget ConstructorChainTarget { get; set; } = ConstructorChainTarget.None;

    public IReadOnlyList<IAstNode> ConstructorChainArgs { get; set; } = [];

    public string? NativeEventName { get; set; }

    public string? NativePropertyName { get; set; }

    public string? AssetName { get; set; }

    public string? NativeCallName { get; set; }

    public List<MemberSymbol> Overloads { get; } = [];
}
