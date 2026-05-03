using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Ast.Expressions;

public sealed record BinaryExpressionNode(
    IAstNode Left,
    string Op,
    IAstNode Right,
    SourceLocation Location) : IAstNode;
