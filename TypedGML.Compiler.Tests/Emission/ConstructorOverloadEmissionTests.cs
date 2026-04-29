using FluentAssertions;
using TypedGML.Compiler.Tests.Helpers;

namespace TypedGML.Compiler.Tests.Emission;

public sealed class ConstructorOverloadEmissionTests
{
    [Fact]
    public void Test_ConstructorOverloads_UniqueFunctionNames()
    {
        var result = CompilerFixture.Compile("""
            public class A {
                public constructor() { }
                public constructor(number x) { }
            }

            public class Host {
                public void Run() {
                    var a0 = new A();
                    var a1 = new A(42);
                }
            }
            """);

        result.HasErrors.Should().BeFalse(ErrorText(result));
        var a = result.GetFile("/A.gml")!;
        var host = result.GetFile("/Host.gml")!;

        GmlAssert.ContainsPattern(a, "function A_create__()");
        GmlAssert.ContainsPattern(a, "function A_create__number(x)");
        GmlAssert.NotContainsPattern(a, "function A_create(");
        GmlAssert.ContainsPattern(host, "var a0 = A_create__();");
        GmlAssert.ContainsPattern(host, "var a1 = A_create__number(42);");
    }

    [Fact]
    public void Test_SingleConstructor_NoSuffix()
    {
        var result = CompilerFixture.Compile("""
            public class B {
                public constructor(number x) { }
            }
            """);

        result.HasErrors.Should().BeFalse(ErrorText(result));
        var b = result.GetFile("/B.gml")!;

        GmlAssert.ContainsPattern(b, "function B_create(x)");
        GmlAssert.NotContainsPattern(b, "function B_create__number");
    }

    [Fact]
    public void Test_RequestedConstructorNames()
    {
        var result = CompilerFixture.Compile("""
            public class Foo {
                public constructor(number x) { }
            }

            public class Bar {
                public constructor() { }
                public constructor(number x) { }
            }
            """);

        result.HasErrors.Should().BeFalse(ErrorText(result));
        GmlAssert.ContainsPattern(result.GetFile("/Foo.gml")!, "function Foo_create(x)");
        GmlAssert.ContainsPattern(result.GetFile("/Bar.gml")!, "function Bar_create__()");
        GmlAssert.ContainsPattern(result.GetFile("/Bar.gml")!, "function Bar_create__number(x)");
    }

    private static string ErrorText(CompileResult result) =>
        string.Join(Environment.NewLine, result.Errors.Select(error => $"{error.Code}: {error.Message}"));
}
