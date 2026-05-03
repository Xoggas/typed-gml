using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Ast.Members;

public sealed record ConversionOperatorNode(
    ConversionOperatorKind ConversionKind,
    string TargetType,
    ParameterNode Parameter,
    IReadOnlyList<string> Modifiers,
    IReadOnlyList<DecoratorNode> Decorators,
    IAstNode? Body,
    DocCommentNode? DocComment,
    SourceLocation Location) : IAstNode;
