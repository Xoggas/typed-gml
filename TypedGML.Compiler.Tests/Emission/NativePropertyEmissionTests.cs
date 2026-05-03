using FluentAssertions;
using TypedGML.Compiler.Tests.Helpers;

namespace TypedGML.Compiler.Tests.Emission;

public sealed class NativePropertyEmissionTests
{
    [Fact]
    public void Test_NativePropertyInsideRegularClassUsesAccessors()
    {
        var result = Compile("""
            public class NativeBacked {
                @NativeProperty("native_value")
                public number Value { get; set; }

                public number Read() {
                    return Value;
                }

                public void Write(number value) {
                    Value = value;
                }
            }
            """);

        result.HasErrors.Should().BeFalse();
        var gml = result.GetFile("NativeBacked.gml")!;

        GmlAssert.ContainsPattern(gml, "return NativeBacked_get_Value(self);");
        GmlAssert.ContainsPattern(gml, "NativeBacked_set_Value(self, value);");
        GmlAssert.ContainsPattern(gml, "return self.native_value;");
        GmlAssert.ContainsPattern(gml, "self.native_value = value;");
    }

    private static CompileResult Compile(string source) => CompilerFixture.Compile(source);
}
