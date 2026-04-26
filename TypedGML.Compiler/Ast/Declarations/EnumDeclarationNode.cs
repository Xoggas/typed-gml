using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Ast.Declarations;

public sealed record EnumDeclarationNode(
    string Name,
    IReadOnlyList<DecoratorNode> Decorators,
    IReadOnlyList<EnumMemberNode> Members,
    DocCommentNode? DocComment,
    SourceLocation Location) : IAstNode;
