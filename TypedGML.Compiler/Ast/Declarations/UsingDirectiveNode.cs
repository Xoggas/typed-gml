using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Ast.Declarations;

public sealed record UsingDirectiveNode(
    string QualifiedName,
    string? Alias,
    DocCommentNode? DocComment,
    SourceLocation Location) : IAstNode;
