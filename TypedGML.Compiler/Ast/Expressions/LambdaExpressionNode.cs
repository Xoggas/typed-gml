using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Ast.Expressions;

public sealed record LambdaExpressionNode(
    IReadOnlyList<ParameterNode> Parameters,
    IAstNode Body,
    SourceLocation Location) : IAstNode;
