namespace TypedGML.Compiler.Tests.Helpers;

public static class PopulationFixture
{
    public static CompileResult Compile(string tgmlSource) =>
        new PopulationPipelineRunner().Compile(tgmlSource);
}
