using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Ast.Expressions;

public sealed record TernaryExpressionNode(
    IAstNode Condition,
    IAstNode ThenExpr,
    IAstNode ElseExpr,
    SourceLocation Location) : IAstNode;
