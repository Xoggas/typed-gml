using TypedGML.Compiler.Tests.Infrastructure;

namespace TypedGML.Compiler.Tests;

public sealed class SemanticCheckTests
{
    [Fact]
    public void VarFromNullReportsAmbiguousInference()
    {
        var result = CompilerTestDriver.Compile(
            """
            public class Sample {
                public void Run() {
                    var value = null;
                }
            }
            """);

        Assert.False(result.Succeeded);
        Assert.Contains("TGML0013", result.StandardError);
    }

    [Fact]
    public void NullAssignedToNonNullableReportsDiagnostic()
    {
        var result = CompilerTestDriver.Compile(
            """
            public class Sample {
                public void Run() {
                    number value = null;
                }
            }
            """);

        Assert.False(result.Succeeded);
        Assert.Contains("TGML0030", result.StandardError);
    }

    [Fact]
    public void ReadonlyWriteOutsideConstructorReportsDiagnostic()
    {
        var result = CompilerTestDriver.Compile(
            """
            public class Sample {
                public readonly number Value;

                public void Run() {
                    Value = 1;
                }
            }
            """);

        Assert.False(result.Succeeded);
        Assert.Contains("TGML0009", result.StandardError);
    }

    [Fact]
    public void DuplicateSwitchCaseReportsDiagnostic()
    {
        var result = CompilerTestDriver.Compile(
            """
            public class Sample {
                public void Run(number value) {
                    switch (value) {
                        case 1:
                            break;
                        case 1:
                            break;
                    }
                }
            }
            """);

        Assert.False(result.Succeeded);
        Assert.Contains("TGML0020", result.StandardError);
    }

    [Fact]
    public void LambdaCaptureReportsDiagnostic()
    {
        var result = CompilerTestDriver.Compile(
            """
            public class Sample {
                public void Run() {
                    number seed = 1;
                    var fn = () => seed;
                }
            }
            """);

        Assert.False(result.Succeeded);
        Assert.Contains("TGML0012", result.StandardError);
    }

    [Fact]
    public void ConcreteNonVoidMethodMustReturnOnAllPaths()
    {
        var result = CompilerTestDriver.Compile(
            """
            public class Sample {
                public number Run(bool flag) {
                    if (flag) {
                        return 1;
                    }
                }
            }
            """);

        Assert.False(result.Succeeded);
        Assert.Contains("TGML0021", result.StandardError);
    }

    [Fact]
    public void ObjectDecoratorRequiresGameObjectBase()
    {
        var result = CompilerTestDriver.Compile(
            """
            @Object("OBJ_Test")
            public class Sample { }
            """);

        Assert.False(result.Succeeded);
        Assert.Contains("TGML0046", result.StandardError);
    }
}
