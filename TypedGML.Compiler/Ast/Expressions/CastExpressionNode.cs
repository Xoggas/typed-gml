using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Ast.Expressions;

public sealed record CastExpressionNode(
    IAstNode Expression,
    string TargetType,
    CastKind CastKind,
    SourceLocation Location) : IAstNode;
