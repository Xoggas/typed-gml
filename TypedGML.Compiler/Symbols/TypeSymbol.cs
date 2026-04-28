using TypedGML.Compiler.Ast.Members;

namespace TypedGML.Compiler.Symbols;

public sealed class TypeSymbol
{
    public string QualifiedName { get; set; } = string.Empty;

    public TypeKind Kind { get; set; }

    public List<MemberSymbol> Members { get; } = [];

    public TypeSymbol? Base { get; set; }

    public List<TypeSymbol> Interfaces { get; } = [];

    public List<GenericParamNode> GenericParameters { get; } = [];

    public bool IsAbstract { get; set; }

    public bool IsSealed { get; set; }

    public string? ObjectAssetName { get; set; }

    public string? BclTypeName { get; set; }
}
