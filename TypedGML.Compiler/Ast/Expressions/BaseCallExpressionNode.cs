using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Ast.Expressions;

public sealed record BaseCallExpressionNode(
    string MemberName,
    IReadOnlyList<IAstNode> Args,
    SourceLocation Location) : IAstNode;
