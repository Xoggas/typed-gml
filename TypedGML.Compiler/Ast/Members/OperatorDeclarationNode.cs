using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Ast.Members;

public sealed record OperatorDeclarationNode(
    string OperatorSymbol,
    IReadOnlyList<string> Modifiers,
    IReadOnlyList<ParameterNode> Parameters,
    string ReturnType,
    IReadOnlyList<DecoratorNode> Decorators,
    IAstNode? Body,
    DocCommentNode? DocComment,
    SourceLocation Location) : IAstNode;
