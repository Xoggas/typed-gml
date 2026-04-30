using FluentAssertions;
using TypedGML.Compiler.Tests.Helpers;

namespace TypedGML.Compiler.Tests.Emission;

public sealed class PrimitiveKeywordStaticAccessEmissionTests
{
    [Fact]
    public void Test_PrimitiveKeyword_StaticMethodCall_LowercaseAlias()
    {
        var result = Compile("""
            using TypedGML.Core;

            public class Host {
                public void Run() {
                    var lower = string.Length("hi");
                    var upper = String.Length("hi");
                }
            }
            """);

        result.HasErrors.Should().BeFalse(ErrorText(result));
        var gml = result.GetFile("Host.gml")!;
        Count(gml, """global.TypedGML_Core_String_Length("hi")""").Should().Be(2);
    }

    [Fact]
    public void Test_PrimitiveKeyword_StaticCall_Number()
    {
        var result = Compile("""
            using TypedGML.Core;

            public class Host {
                public void Run() {
                    var lower = number.Parse("1");
                    var upper = Number.Parse("1");
                }
            }
            """);

        result.HasErrors.Should().BeFalse(ErrorText(result));
        var gml = result.GetFile("Host.gml")!;
        Count(gml, """global.TypedGML_Core_Number_Parse("1")""").Should().Be(2);
    }

    [Fact]
    public void Test_PrimitiveKeyword_StaticCall_Bool()
    {
        var result = Compile("""
            using TypedGML.Core;

            public class Host {
                public void Run() {
                    var lower = bool.Parse("true");
                    var upper = Bool.Parse("true");
                }
            }
            """);

        result.HasErrors.Should().BeFalse(ErrorText(result));
        var gml = result.GetFile("Host.gml")!;
        Count(gml, """global.TypedGML_Core_Bool_Parse("true")""").Should().Be(2);
    }

    private static CompileResult Compile(string source) =>
        CompilerFixture.Compile(source);

    private static int Count(string text, string value) =>
        text.Split(value, StringSplitOptions.None).Length - 1;

    private static string ErrorText(CompileResult result) =>
        string.Join(Environment.NewLine, result.Errors.Select(error => $"{error.Code}: {error.Message}"));
}
