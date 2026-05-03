using FluentAssertions;
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
            """).GetFile("NullableHost.gml")!, "(obj != undefined ? Target_Method(obj) : undefined)");

    [Fact]
    public void Test_NullConditionalMethodCallInfersNullableReturn()
    {
        var result = Compile("""
            public struct MyStruct {
                public number GetValue() {
                    return 1;
                }
            }
            public class NullableHost {
                public void Run() {
                    MyStruct? s = null;
                    var x = s?.GetValue();
                }
            }
            """);

        result.HasErrors.Should().BeFalse(string.Join(Environment.NewLine, result.Errors.Select(e => $"{e.Code}: {e.Message}")));
        GmlAssert.ContainsPattern(result.GetFile("NullableHost.gml")!, "var x = (s != undefined ? MyStruct_GetValue(s) : undefined);");
    }

    [Fact]
    public void Test_NullConditionalAndCoalescingCacheNonTrivialOperands()
    {
        var result = Compile("""
            public class BankAccount {
                public string Owner { get; }
            }
            public class Bank {
                public BankAccount? FindAccount(string id) {
                    return null;
                }
            }
            public class NullableHost {
                private Bank _bank;
                public void Run() {
                    string owner = _bank.FindAccount("ACC-001")?.Owner ?? "unknown";
                }
            }
            """);

        result.HasErrors.Should().BeFalse(string.Join(Environment.NewLine, result.Errors.Select(e => $"{e.Code}: {e.Message}")));
        var gml = result.GetFile("NullableHost.gml")!;
        gml.Split("Bank_FindAccount", StringSplitOptions.None).Length.Should().Be(2);
        GmlAssert.ContainsPattern(gml, "var __tmp_0 = Bank_FindAccount(self._bank, \"ACC-001\");");
        GmlAssert.ContainsPattern(gml, "var __tmp_1 = (__tmp_0 != undefined ? BankAccount_get_Owner(__tmp_0) : undefined);");
        GmlAssert.ContainsPattern(gml, "var owner = (__tmp_1 != undefined ? __tmp_1 : \"unknown\");");
    }

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
