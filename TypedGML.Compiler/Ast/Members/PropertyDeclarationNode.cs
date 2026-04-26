using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Ast.Members;

public sealed record PropertyDeclarationNode(
    string Name,
    string TypeRef,
    IReadOnlyList<string> Modifiers,
    IReadOnlyList<DecoratorNode> Decorators,
    IReadOnlyList<AccessorNode> Accessors,
    DocCommentNode? DocComment,
    SourceLocation Location) : IAstNode;
