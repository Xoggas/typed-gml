using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Tests.Helpers;

public static class PopulationFixture
{
    public static CompileResult Compile(string tgmlSource) =>
        new PopulationPipelineRunner().Compile(tgmlSource);

    public static (CompileResult Result, SymbolTable Symbols) CompileWithSymbols(string tgmlSource) =>
        new PopulationPipelineRunner().PopulateWithSymbols(tgmlSource);
}
