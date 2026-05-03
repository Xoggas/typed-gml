using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Ast.Expressions;

public sealed record UnaryExpressionNode(
    string Op,
    IAstNode Operand,
    SourceLocation Location) : IAstNode;
