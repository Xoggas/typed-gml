using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Ast.Expressions;

public sealed record NullCoalescingExpressionNode(
    IAstNode Left,
    IAstNode Right,
    SourceLocation Location) : IAstNode;
