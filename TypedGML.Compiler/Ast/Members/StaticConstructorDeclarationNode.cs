using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Ast.Members;

public sealed record StaticConstructorDeclarationNode(
    string TypeName,
    IAstNode Body,
    SourceLocation Location) : IAstNode;
