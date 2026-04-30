using FluentAssertions;
using TypedGML.Compiler.Tests.Helpers;

namespace TypedGML.Compiler.Tests.Emission;

public sealed class PrimitiveOperatorTests
{
    [Theory]
    [InlineData("number x = 1 + 2;", "1 + 2")]
    [InlineData("number x = 1 - 2;", "1 - 2")]
    [InlineData("number x = 1 * 2;", "1 * 2")]
    [InlineData("number x = 1 / 2;", "1 / 2")]
    [InlineData("number x = 1 % 2;", "1 % 2")]
    [InlineData("bool b = 1 < 2;", "1 < 2")]
    [InlineData("bool b = 1 > 2;", "1 > 2")]
    [InlineData("bool b = 1 <= 2;", "1 <= 2")]
    [InlineData("bool b = 1 >= 2;", "1 >= 2")]
    [InlineData("bool b = 1 == 2;", "1 == 2")]
    [InlineData("bool b = 1 != 2;", "1 != 2")]
    [InlineData("string s = \"a\" + \"b\";", "\"a\" + \"b\"")]
    [InlineData("bool b = \"a\" == \"b\";", "\"a\" == \"b\"")]
    [InlineData("bool b = \"a\" != \"b\";", "\"a\" != \"b\"")]
    [InlineData("string s = \"x=\" + 1;", "\"x=\" + string(1)")]
    [InlineData("string s = 1 + \"px\";", "string(1) + \"px\"")]
    [InlineData("string s = \"enabled=\" + true;", "\"enabled=\" + string(true)")]
    [InlineData("string s = false + \"!\";", "string(false) + \"!\"")]
    [InlineData("bool b = true == false;", "true == false")]
    [InlineData("bool b = true != false;", "true != false")]
    public void Test_PrimitiveOperators_EmitIntrinsicForms(string statement, string expected)
    {
        var result = CompileInMethod(statement);

        result.HasErrors.Should().BeFalse();
        var gml = result.GetFile("OperatorHost.gml")!;
        GmlAssert.ContainsPattern(gml, expected);
        GmlAssert.NotContainsPattern(gml, "__op_");
    }

    [Fact]
    public void Test_PrimitiveOperators_UseBclAliasesWhenTypeNamesAreShadowed()
    {
        var result = CompilerFixture.Compile("""
            public class Number { }
            public class String { }
            public class Bool { }
            public class OperatorHost {
                public void Run() {
                    number sum = 1 + 2;
                    bool less = 1 < 2;
                    string text = "x=" + 1;
                }
            }
            """);

        result.HasErrors.Should().BeFalse();
        var gml = result.GetFile("OperatorHost.gml")!;
        GmlAssert.ContainsPattern(gml, "1 + 2");
        GmlAssert.ContainsPattern(gml, "1 < 2");
        GmlAssert.ContainsPattern(gml, "\"x=\" + string(1)");
    }

    [Fact] public void Test_StringPlusNumberPlusString_FlatChain() { var gml = CompileInMethod("string s = \"a\" + 1 + \"b\";").GetFile("OperatorHost.gml")!; GmlAssert.ContainsPattern(gml, """(("a" + string(1) + "b"))"""); GmlAssert.NotContainsPattern(gml, """(("a" + string(1)) + "b")"""); }
    [Fact] public void Test_NumberPlusStringPlusNumber_FlatChain() { var gml = CompileInMethod("string s = 1 + \"a\" + 2;").GetFile("OperatorHost.gml")!; GmlAssert.ContainsPattern(gml, """((string(1) + "a" + string(2)))"""); GmlAssert.NotContainsPattern(gml, """((string(1) + "a") + 2)"""); }
    [Fact] public void Test_BoolPlusStringPlusBool_FlatChain() { var gml = CompileInMethod("string s = true + \"a\" + false;").GetFile("OperatorHost.gml")!; GmlAssert.ContainsPattern(gml, """((string(true) + "a" + string(false)))"""); GmlAssert.NotContainsPattern(gml, """((string(true) + "a") + false)"""); }
    [Fact] public void Test_BoolLogical_NotOperator() { var gml = CompileInMethod("bool b = not true;").GetFile("OperatorHost.gml")!; GmlAssert.ContainsPattern(gml, "!true"); GmlAssert.NotContainsPattern(gml, "not true"); }

    private static CompileResult CompileInMethod(string statement) =>
        CompilerFixture.Compile($$"""
            public class OperatorHost {
                public void Run() {
                    {{statement}}
                }
            }
            """);
}
