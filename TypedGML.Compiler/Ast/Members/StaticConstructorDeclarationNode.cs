using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Ast.Members;

public sealed record StaticConstructorDeclarationNode(
    string TypeName,
    IReadOnlyList<ParameterNode> Parameters,
    IAstNode Body,
    SourceLocation Location) : IAstNode;
