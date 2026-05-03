using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Ast.Expressions;

public sealed record NullConditionalExpressionNode(
    IAstNode Target,
    string MemberName,
    SourceLocation Location) : IAstNode;
