using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Ast.Expressions;

public sealed record InvocationExpressionNode(
    IAstNode Target,
    IReadOnlyList<IAstNode> PositionalArgs,
    IReadOnlyList<NamedArgNode> NamedArgs,
    SourceLocation Location) : IAstNode;
