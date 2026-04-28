using FluentAssertions;
using TypedGML.Compiler.Diagnostics;
using TypedGML.Compiler.Tests.Helpers;

namespace TypedGML.Compiler.Tests.Emission;

public sealed class OverloadEmissionTests
{
    [Fact]
    public void Test_OverloadCallWithNumberUsesNumberSuffix()
    {
        var result = CompileWithCall("Foo(42);");

        result.HasErrors.Should().BeFalse();
        var gml = result.GetFile("/T.gml")!;
        GmlAssert.ContainsPattern(gml, "T_Foo__number(self, 42);");
        GmlAssert.NotContainsPattern(gml, "T_Foo__string(self, 42);");
    }

    [Fact]
    public void Test_OverloadCallWithStringUsesStringSuffix()
    {
        var result = CompileWithCall("""Foo("hello");""");

        result.HasErrors.Should().BeFalse();
        var gml = result.GetFile("/T.gml")!;
        GmlAssert.ContainsPattern(gml, """T_Foo__string(self, "hello");""");
        GmlAssert.NotContainsPattern(gml, """T_Foo__number(self, "hello");""");
    }

    [Fact]
    public void Test_OverloadCallWithBoolReportsNoMatch()
    {
        var result = CompileWithCall("Foo(true);");

        result.HasError(DiagnosticCode.NoMatchingMethodOverload).Should().BeTrue(
            string.Join(Environment.NewLine, result.Errors.Select(error => $"{error.Code}: {error.Message}")));
    }

    private static CompileResult CompileWithCall(string call) =>
        CompilerFixture.Compile($$"""
            public class T {
                public void Foo(number value) { }
                public void Foo(string value) { }
                public void Bar() {
                    {{call}}
                }
            }
            """);
}
