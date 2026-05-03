using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Ast.Expressions;

public sealed record NameofExpressionNode(
    IReadOnlyList<string> Chain,
    SourceLocation Location) : IAstNode;
