using TypedGML.Compiler.Tests.Infrastructure;

namespace TypedGML.Compiler.Tests;

public sealed class SpecificationWarningTests
{
    [Fact]
    public void UnreachableStatementsProduceWarningsWithoutFailingCompilation()
    {
        var result = CompilerAssert.Succeeds(
            """
            public class Sample {
                public number Run() {
                    return 1;
                    var later = 2;
                }
            }
            """);

        CompilerAssert.ErrorContains(result, "warning TGML0022", "Statement is unreachable.");
    }

    [Fact]
    public void NonVoidSwitchWithoutDefaultProducesWarning()
    {
        var result = CompilerAssert.Succeeds(
            """
            public class Sample {
                public number Run(number value) {
                    switch (value) {
                        case 1:
                            return 1;
                    }

                    return 0;
                }
            }
            """);

        CompilerAssert.ErrorContains(result, "warning TGML0021", "Non-void switch should include a default section");
    }
}
