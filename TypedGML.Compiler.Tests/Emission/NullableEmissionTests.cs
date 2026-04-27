using TypedGML.Compiler.Tests.Helpers;

namespace TypedGML.Compiler.Tests.Emission;

public sealed class NullableEmissionTests
{
    [Fact]
    public void Test_NullableDeclaration() =>
        GmlAssert.ContainsPattern(CompileInMethod("number? n = null;").GetFile("NullableHost.gml")!, "var n = undefined;");

    [Fact]
    public void Test_NullCoalescing() =>
        GmlAssert.ContainsPattern(CompileInMethod("number? n = null; number value = n ?? 0;").GetFile("NullableHost.gml")!, "(n != undefined ? n : 0)");

    [Fact]
    public void Test_NullConditionalMemberAccess() =>
        GmlAssert.ContainsPattern(Compile("""
            public class Target { public number Field; }
            public class NullableHost {
                public void Run() {
                    Target? obj = null;
                    var value = obj?.Field;
                }
            }
            """).GetFile("NullableHost.gml")!, "(obj != undefined ? obj.Field : undefined)");

    [Fact]
    public void Test_NullConditionalMethodCall() =>
        GmlAssert.ContainsPattern(Compile("""
            public class Target { public void Method() { } }
            public class NullableHost {
                public void Run() {
                    Target? obj = null;
                    obj?.Method();
                }
            }
            """).GetFile("NullableHost.gml")!, "(obj != undefined ? obj.Method() : undefined)");

    private static CompileResult Compile(string source) => CompilerFixture.Compile(source);

    private static CompileResult CompileInMethod(string statement) =>
        Compile($$"""
            public class NullableHost {
                public void Run() {
                    {{statement}}
                }
            }
            """);
}
