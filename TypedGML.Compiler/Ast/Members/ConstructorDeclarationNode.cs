using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Ast.Members;

public sealed record ConstructorDeclarationNode(
    IReadOnlyList<string> Modifiers,
    IReadOnlyList<ParameterNode> Parameters,
    IReadOnlyList<DecoratorNode> Decorators,
    ConstructorChainTarget ChainTarget,
    IReadOnlyList<IAstNode> ChainArgs,
    IAstNode Body,
    DocCommentNode? DocComment,
    SourceLocation Location) : IAstNode;
