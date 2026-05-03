using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Ast.Declarations;

public sealed record NamespaceDeclarationNode(
    string Name,
    IReadOnlyList<IAstNode> Body,
    bool IsFileScoped,
    DocCommentNode? DocComment,
    SourceLocation Location) : IAstNode;
