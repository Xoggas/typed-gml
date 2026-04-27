using TypedGML.Compiler.Tests.Helpers;

namespace TypedGML.Compiler.Tests.Emission;

public sealed class PrimitiveOperatorTests
{
    [Fact] public void Test_NumberAddition_Infix() { var gml = CompileInMethod("number x = 1 + 2;").GetFile("OperatorHost.gml")!; GmlAssert.ContainsPattern(gml, "1 + 2"); GmlAssert.NotContainsPattern(gml, "__op_add"); }
    [Fact] public void Test_NumberComparison_Infix() => GmlAssert.ContainsPattern(CompileInMethod("bool b = 1 == 2;").GetFile("OperatorHost.gml")!, "1 == 2");
    [Fact] public void Test_StringConcat_Infix() => GmlAssert.ContainsPattern(CompileInMethod("string s = \"a\" + \"b\";").GetFile("OperatorHost.gml")!, "\"a\" + \"b\"");
    [Fact] public void Test_StringPlusNumber_Shim() => GmlAssert.ContainsPattern(CompileInMethod("string s = \"x=\" + 1;").GetFile("OperatorHost.gml")!, "\"x=\" + string(1)");
    [Fact] public void Test_StringPlusNumberPlusString_FlatChain() { var gml = CompileInMethod("string s = \"a\" + 1 + \"b\";").GetFile("OperatorHost.gml")!; GmlAssert.ContainsPattern(gml, """(("a" + string(1) + "b"))"""); GmlAssert.NotContainsPattern(gml, """(("a" + string(1)) + "b")"""); }
    [Fact] public void Test_NumberPlusStringPlusNumber_FlatChain() { var gml = CompileInMethod("string s = 1 + \"a\" + 2;").GetFile("OperatorHost.gml")!; GmlAssert.ContainsPattern(gml, """((string(1) + "a" + string(2)))"""); GmlAssert.NotContainsPattern(gml, """((string(1) + "a") + 2)"""); }
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
