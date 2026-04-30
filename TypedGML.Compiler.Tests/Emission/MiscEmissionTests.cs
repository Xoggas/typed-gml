using TypedGML.Compiler.Tests.Helpers;

namespace TypedGML.Compiler.Tests.Emission;

public sealed class MiscEmissionTests
{
    [Fact]
    public void Test_TypeofPrimitive() =>
        GmlAssert.ContainsPattern(CompileInMethod("var name = typeof(number);").GetFile("MiscHost.gml")!, "\"number\"");

    [Fact]
    public void Test_TypeofClass() =>
        GmlAssert.ContainsPattern(Compile("""
            public class MyClass { }
            public class MiscHost {
                public void Run() {
                    var name = typeof(MyClass);
                }
            }
            """).GetFile("MiscHost.gml")!, "\"MyClass\"");

    [Fact]
    public void Test_NameofMember() =>
        GmlAssert.ContainsPattern(Compile("""
            public class MyClass { public static void MyMethod() { } }
            public class MiscHost {
                public void Run() {
                    var name = nameof(MyClass.MyMethod);
                }
            }
            """).GetFile("MiscHost.gml")!, "\"MyMethod\"");

    [Fact]
    public void Test_DictionaryLiteral()
    {
        var gml = Compile("""
            using TypedGML.Collections;
            public class MiscHost {
                public void Run() {
                    Dictionary<string, number> scores = {"a": 1};
                }
            }
            """).GetFile("MiscHost.gml")!;
        GmlAssert.ContainsPattern(gml, "TypedGML_Collections_Dictionary2_create()");
        GmlAssert.ContainsPattern(gml, "TypedGML_Collections_Dictionary2_Add(");
        GmlAssert.NotContainsPattern(gml, "ds_map_");
    }

    [Fact]
    public void Test_DictionaryLiteralEmpty()
    {
        var gml = Compile("""
            using TypedGML.Collections;
            public class MiscHost {
                public void Run() {
                    Dictionary<string, number> scores = {};
                }
            }
            """).GetFile("MiscHost.gml")!;
        GmlAssert.ContainsPattern(gml, "TypedGML_Collections_Dictionary2_create()");
        GmlAssert.NotContainsPattern(gml, "(function()");
    }

    [Fact]
    public void Test_ArrayLiteral() =>
        GmlAssert.ContainsPattern(CompileInMethod("var values = [1, 2, 3];").GetFile("MiscHost.gml")!, "[1, 2, 3]");

    [Fact]
    public void Test_GenericArgs() =>
        GmlAssert.ContainsPattern(Compile("""
            public class Container<T> { }
            public class MiscHost {
                public void Run() {
                    var value = new Container<number>();
                }
            }
            """).GetFile("MiscHost.gml")!, "__inst.__genericArgs = { T: \"number\" };");

    [Fact]
    public void Test_GmlFormatting_FourSpaceIndent()
    {
        var result = Compile("""
            using TypedGML.Collections;
            public class FormatTarget {
                public static number X = 5;
                public number Value { get; set; }
                public void Run(number? n) {
                    Dictionary<string, number> scores = {"a": 1};
                    if (n != null) {
                        Value = n ?? 0;
                    }
                }
            }
            """);
        foreach (var gml in result.Output.Values)
            GmlAssert.IsFormattedCorrectly(gml);
    }

    [Fact]
    public void Test_BclHashSetToString_StringConcatChain()
    {
        var result = Compile("""
            using TypedGML.Collections;
            public class MiscHost {
                public void Run() {
                    var set = new HashSet<number>();
                }
            }
            """);
        var gml = result.GetFile("TypedGML_Collections_HashSet1.gml")!;
        GmlAssert.ContainsPattern(gml, """(("HashSet[" + string(TypedGML_Collections_HashSet1_get_Count(self)) + "]"))""");
        GmlAssert.NotContainsPattern(gml, """(("HashSet[" + string(TypedGML_Collections_HashSet1_get_Count(self))) + "]")""");
    }

    [Fact] public void Test_Lambda_Action_NoReturn() { var gml = CompileInMethod("Action<number> fn = (number x) => x * 2;").GetFile("MiscHost.gml")!; GmlAssert.ContainsPattern(gml, "var fn = function(x) { (x * 2); };"); GmlAssert.NotContainsPattern(gml, "return (x * 2);"); }

    [Fact] public void Test_Lambda_Func_HasReturn_ExprForm() =>
        GmlAssert.ContainsPattern(
            CompileInMethod("Func<number, number> fn = (number x) => x * 2;").GetFile("MiscHost.gml")!,
            "var fn = function(x) { return (x * 2); };");

    [Fact] public void Test_Lambda_Func_HasReturn_BlockForm() { var gml = CompileInMethod("Func<number, number> fn = (number x) => { var y = x + 1; return y; };").GetFile("MiscHost.gml")!; GmlAssert.ContainsPattern(gml, "var fn = function(x)"); GmlAssert.ContainsPattern(gml, "var y = (x + 1);"); GmlAssert.ContainsPattern(gml, "return y;"); }

    [Fact] public void Test_Lambda_Action_NoArgs() { var gml = CompileInMethod("Action fn = () => { var x = 1; };").GetFile("MiscHost.gml")!; GmlAssert.ContainsPattern(gml, "var fn = function()"); GmlAssert.ContainsPattern(gml, "var x = 1;"); }

    [Fact] public void Test_Lambda_Func_NoArgs() =>
        GmlAssert.ContainsPattern(
            CompileInMethod("Func<number> fn = () => 42;").GetFile("MiscHost.gml")!,
            "var fn = function() { return 42; };");

    private static CompileResult Compile(string source) => CompilerFixture.Compile(source);

    private static CompileResult CompileInMethod(string statement) =>
        Compile($$"""
            public class MiscHost {
                public void Run() {
                    {{statement}}
                }
            }
            """);
}
