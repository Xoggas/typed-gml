using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Ast.Declarations;

public sealed record ClassDeclarationNode(
    string Name,
    IReadOnlyList<string> Modifiers,
    IReadOnlyList<GenericParamNode> GenericParams,
    IReadOnlyList<string> BaseTypes,
    IReadOnlyList<DecoratorNode> Decorators,
    IReadOnlyList<IAstNode> Members,
    DocCommentNode? DocComment,
    SourceLocation Location) : IAstNode;
