using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Ast.Expressions;

public sealed record ThisExpressionNode(SourceLocation Location) : IAstNode;

