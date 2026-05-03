using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Ast.Declarations;

public sealed record DelegateDeclarationNode(
    string Name,
    IReadOnlyList<string> Modifiers,
    IReadOnlyList<GenericParamNode> GenericParams,
    string ReturnType,
    IReadOnlyList<ParameterNode> Parameters,
    IReadOnlyList<DecoratorNode> Decorators,
    DocCommentNode? DocComment,
    SourceLocation Location) : IAstNode;
