using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Ast.Expressions;

public sealed record LiteralExpressionNode(
    object? Value,
    LiteralKind Kind,
    SourceLocation Location) : IAstNode;
