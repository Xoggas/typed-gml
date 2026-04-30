using FluentAssertions;
using TypedGML.Compiler.Diagnostics;
using TypedGML.Compiler.Tests.Helpers;

namespace TypedGML.Compiler.Tests.Verification;

public sealed class TypeSystemTests
{
    [Fact] public void Test_NumberPlusBool_TGML0022() => CompileInMethod("number x = 1 + true;").HasError(DiagnosticCode.TypeMismatch).Should().BeTrue();
    [Fact] public void Test_NumberAndBool_Valid() => CompileInMethod("bool b = true and false;").HasErrors.Should().BeFalse();
    [Fact] public void Test_NumberUsedAsBoolCondition_TGML0019() => CompileInMethod("number n = 1; if (n) { }").HasError(DiagnosticCode.ImplicitNumberBoolConversion).Should().BeTrue();
    [Fact] public void Test_NullAssignedToNonNullable_TGML0030() => CompileInMethod("number x = null;").HasError(DiagnosticCode.NullAssignedToNonNullableType).Should().BeTrue();
    [Fact] public void Test_NullAssignedToNullable_Valid() => CompileInMethod("number? x = null;").HasErrors.Should().BeFalse();
    [Fact] public void Test_NullableUsedAsNonNullable_TGML0022() => CompileInMethod("number? x = null; number y = x;").HasError(DiagnosticCode.TypeMismatch).Should().BeTrue();
    [Fact] public void Test_NullCoalescingUnwraps() => CompileInMethod("number? x = null; number y = x ?? 0;").HasErrors.Should().BeFalse();
    [Fact] public void Test_StringPlusNumber_Valid() => CompileInMethod("string s = \"x=\" + 1;").HasErrors.Should().BeFalse();
    [Fact] public void Test_BoolPlusNumber_TGML0022() => CompileInMethod("string s = true + 1;").HasError(DiagnosticCode.TypeMismatch).Should().BeTrue();
    [Fact] public void Test_VarInferredFromLiteral() => CompileInMethod("var x = 42;").HasErrors.Should().BeFalse();
    [Fact] public void Test_VarFromNull_TGML0013() => CompileInMethod("var x = null;").HasError(DiagnosticCode.AmbiguousVarInference).Should().BeTrue();
    [Fact] public void Test_AssignWrongType_TGML0022() => CompileInMethod("number x = \"hello\";").HasError(DiagnosticCode.TypeMismatch).Should().BeTrue();
    [Fact] public void Test_ArrayLiteral_WrongElementType_Error() => CompileInMethod("number[] x = [\"hello\"];").HasError(DiagnosticCode.TypeMismatch).Should().BeTrue();
    [Fact] public void Test_ArrayLiteral_CorrectElementType_Valid() => CompileInMethod("number[] x = [1, 2, 3];").HasErrors.Should().BeFalse();
    [Fact] public void Test_EmptyLiteralAssignedToList_Valid() => CompileInMethod("using TypedGML.Collections;", "List<number> x = [];").HasErrors.Should().BeFalse();
    [Fact] public void Test_EmptyLiteralAssignedToArray_Valid() => CompileInMethod("number[] x = [];").HasErrors.Should().BeFalse();

    [Fact]
    public void Test_ArrayLiteral_FieldWrongElementType_Error() => Compile("""
        using TypedGML.Core;
        public class VerificationHost {
            public number[] Positions = ["hello"];
        }
        """).HasError(DiagnosticCode.TypeMismatch).Should().BeTrue();

    [Fact] public void Test_ImplicitConversion_Valid() => Compile("""
        public class WrappedNumber {
            public constructor() { }
            public static implicit operator WrappedNumber(number n) { return new WrappedNumber(); }
        }
        public class VerificationHost {
            public void Run() { WrappedNumber value = 1; }
        }
        """).HasErrors.Should().BeFalse();

    [Fact]
    public void Test_DefaultNumber()
    {
        var result = CompileDefault("DefaultNumberHost", "number value_default_number = default(number);");
        result.HasErrors.Should().BeFalse();
        GmlAssert.ContainsPattern(result.GetFile("DefaultNumberHost.gml")!, "var value_default_number = 0;");
    }

    [Fact]
    public void Test_DefaultBool()
    {
        var result = CompileDefault("DefaultBoolHost", "bool value_default_bool = default(bool);");
        result.HasErrors.Should().BeFalse();
        GmlAssert.ContainsPattern(result.GetFile("DefaultBoolHost.gml")!, "var value_default_bool = false;");
    }

    [Fact]
    public void Test_DefaultString()
    {
        var result = CompileDefault("DefaultStringHost", "string value_default_string = default(string);");
        result.HasErrors.Should().BeFalse();
        GmlAssert.ContainsPattern(result.GetFile("DefaultStringHost.gml")!, "var value_default_string = undefined;");
    }

    private static CompileResult CompileInMethod(string statement) =>
        Compile($$"""
            using TypedGML.Core;
            public class VerificationHost {
                public void Run() {
                    {{statement}}
                }
            }
            """);

    private static CompileResult CompileInMethod(string usingDirective, string statement) =>
        Compile($$"""
            using TypedGML.Core;
            {{usingDirective}}
            public class VerificationHost {
                public void Run() {
                    {{statement}}
                }
            }
            """);

    private static CompileResult CompileDefault(string className, string statement) =>
        Compile($$"""
            using TypedGML.Core;
            public class {{className}} {
                public void Run() {
                    {{statement}}
                }
            }
            """);

    private static CompileResult Compile(string source) =>
        CompilerFixture.Compile(source);
}
