using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Ast.Expressions;

public sealed record ArrayLiteralExpressionNode(
    IReadOnlyList<IAstNode> Elements,
    SourceLocation Location) : IAstNode;
