using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Emission.Emitters;

internal sealed record ConstructorInlineFrame(
    TypeSymbol Type,
    IAstNode? Body,
    IReadOnlyList<ConstructorArgumentTemp> Temps);
