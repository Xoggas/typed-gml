namespace TypedGML.Compiler.Tests.Helpers;

public static class CompilerFixture
{
    public static CompileResult Compile(string tgmlSource) =>
        new CompilerPipelineRunner().Compile(tgmlSource);
}
