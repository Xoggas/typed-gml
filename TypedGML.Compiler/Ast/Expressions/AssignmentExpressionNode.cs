using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Ast.Expressions;

public sealed record AssignmentExpressionNode(
    IAstNode Target,
    string Op,
    IAstNode Value,
    SourceLocation Location) : IAstNode;
