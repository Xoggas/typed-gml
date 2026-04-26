using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Ast.Declarations;

public sealed record EnumMemberNode(
    string Name,
    IReadOnlyList<DecoratorNode> Decorators,
    IAstNode? Value,
    DocCommentNode? DocComment,
    SourceLocation Location) : IAstNode;
