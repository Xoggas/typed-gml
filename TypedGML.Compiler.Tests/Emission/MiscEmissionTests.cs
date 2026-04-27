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
        GmlAssert.ContainsPattern(gml, "Dictionary_create()");
        GmlAssert.ContainsPattern(gml, "Dictionary_Add(");
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
        GmlAssert.ContainsPattern(gml, "Dictionary_create()");
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
            """).GetFile("MiscHost.gml")!, "__inst.__genericArgs = { T: \"number\" }");

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
