using FluentAssertions;
using TypedGML.Compiler.Tests.Helpers;

namespace TypedGML.Compiler.Tests.Emission;

public sealed class GenericEmissionTests
{
    [Fact]
    public void Test_GenericConstructorSubstitutesTypeArguments()
    {
        var result = Compile("""
            public class Box<T> {
                public T Value;
                public constructor(T value) {
                    Value = value;
                }
            }
            public class Host {
                public void Run() {
                    var b = new Box<number>(42);
                }
            }
            """);

        result.HasErrors.Should().BeFalse();
        var gml = result.GetFile("Host.gml")!;
        GmlAssert.ContainsPattern(gml, "var b = (function() { var __inst = Box1_create(42); __inst.__genericArgs = { T: \"number\" }; return __inst; })();");
    }

    [Fact]
    public void Test_TypeofGenericTypeParameterReadsGenericArgs()
    {
        var result = Compile("""
            public class Box<T> {
                public string TypeName() {
                    return typeof(T);
                }
            }
            """);

        result.HasErrors.Should().BeFalse();
        var gml = result.GetFile("Box1.gml")!;
        GmlAssert.ContainsPattern(gml, "return self.__genericArgs.T;");
    }

    private static CompileResult Compile(string source) => CompilerFixture.Compile(source);
}
