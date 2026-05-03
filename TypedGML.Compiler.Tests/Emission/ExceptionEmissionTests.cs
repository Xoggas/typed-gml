using FluentAssertions;
using TypedGML.Compiler.Tests.Helpers;

namespace TypedGML.Compiler.Tests.Emission;

public sealed class ExceptionEmissionTests
{
    [Fact]
    public void Test_ThrowNewException_EmitsStringThrow()
    {
        var result = Compile("""
            public class Host {
                public void Run() {
                    throw new Exception("oops");
                }
            }
            """);

        result.HasErrors.Should().BeFalse(ErrorText(result));
        var gml = result.GetFile("Host.gml")!;
        GmlAssert.ContainsPattern(gml, """throw "oops";""");
        GmlAssert.NotContainsPattern(gml, "Exception_create");
    }

    [Fact]
    public void Test_CatchExceptionProperty_EmitsNativeFieldRead()
    {
        var result = Compile("""
            public class Host {
                public void Run() {
                    try {
                        throw new Exception("test");
                    } catch (Exception e) {
                        var msg = e.Message;
                        var raw = e.message;
                    }
                }
            }
            """);

        result.HasErrors.Should().BeFalse(ErrorText(result));
        var gml = result.GetFile("Host.gml")!;
        GmlAssert.ContainsPattern(gml, """throw "test";""");
        GmlAssert.ContainsPattern(gml, "var msg = e.message;");
        GmlAssert.ContainsPattern(gml, "var raw = e.message;");
        GmlAssert.NotContainsPattern(gml, "Exception_get_Message");
    }

    private static CompileResult Compile(string source) => CompilerFixture.Compile(source);

    private static string ErrorText(CompileResult result) =>
        string.Join(Environment.NewLine, result.Errors.Select(error => $"{error.Code}: {error.Message}"));
}
