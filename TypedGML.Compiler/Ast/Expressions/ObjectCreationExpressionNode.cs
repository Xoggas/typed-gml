using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Ast.Expressions;

public sealed record ObjectCreationExpressionNode(
    string TypeRef,
    IReadOnlyList<string> TypeArgs,
    IReadOnlyList<IAstNode> PositionalArgs,
    IReadOnlyList<NamedArgNode> NamedArgs,
    SourceLocation Location) : IAstNode;
