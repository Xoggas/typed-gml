using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Ast.Members;

public sealed record FieldDeclarationNode(
    string Name,
    string TypeRef,
    IReadOnlyList<string> Modifiers,
    IReadOnlyList<DecoratorNode> Decorators,
    IAstNode? Initializer,
    DocCommentNode? DocComment,
    SourceLocation Location) : IAstNode;
