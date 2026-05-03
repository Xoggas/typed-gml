using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Ast.Members;

public sealed record MethodDeclarationNode(
    string Name,
    string TypeRef,
    IReadOnlyList<string> Modifiers,
    IReadOnlyList<GenericParamNode> GenericParams,
    IReadOnlyList<ParameterNode> Parameters,
    IReadOnlyList<DecoratorNode> Decorators,
    IAstNode? Body,
    DocCommentNode? DocComment,
    SourceLocation Location) : IAstNode;
