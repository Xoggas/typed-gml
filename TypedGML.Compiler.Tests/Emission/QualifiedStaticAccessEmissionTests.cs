using FluentAssertions;
using TypedGML.Compiler.Tests.Helpers;

namespace TypedGML.Compiler.Tests.Emission;

public sealed class QualifiedStaticAccessEmissionTests
{
    [Fact]
    public void Test_FullyQualifiedStaticCall_NoUsing()
    {
        var result = CompilerFixture.Compile("""
            public class Host {
                public void Run() {
                    TypedGML.Utils.Debug.Log("hi");
                    var value = TypedGML.Utils.Math.Abs(-1);
                    var pi = TypedGML.Utils.Math.PI;
                }
            }
            """);

        result.HasErrors.Should().BeFalse(ErrorText(result));
        var gml = result.GetFile("/Host.gml")!;

        GmlAssert.ContainsPattern(gml, """global.TypedGML_Utils_Debug_Log("hi");""");
        GmlAssert.ContainsPattern(gml, "var value = global.TypedGML_Utils_Math_Abs(-1);");
        GmlAssert.ContainsPattern(gml, "var pi = TypedGML_Utils_Math_PI;");
    }

    [Fact]
    public void Test_FullyQualifiedStaticCall_WithUsing()
    {
        var result = CompilerFixture.Compile("""
            using TypedGML.Utils;

            public class Host {
                public void Run() {
                    Debug.Log("short");
                    TypedGML.Utils.Debug.Log("full");
                }
            }
            """);

        result.HasErrors.Should().BeFalse(ErrorText(result));
        var gml = result.GetFile("/Host.gml")!;

        GmlAssert.ContainsPattern(gml, """global.TypedGML_Utils_Debug_Log("short");""");
        GmlAssert.ContainsPattern(gml, """global.TypedGML_Utils_Debug_Log("full");""");
    }

    private static string ErrorText(CompileResult result) =>
        string.Join(Environment.NewLine, result.Errors.Select(error => $"{error.Code}: {error.Message}"));
}
