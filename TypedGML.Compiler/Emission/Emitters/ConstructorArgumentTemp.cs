using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Members;

namespace TypedGML.Compiler.Emission.Emitters;

internal sealed record ConstructorArgumentTemp(
    string Name,
    ParameterNode Parameter,
    IAstNode Value);
