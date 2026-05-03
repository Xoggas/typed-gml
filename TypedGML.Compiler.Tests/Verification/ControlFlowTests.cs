using FluentAssertions;
using TypedGML.Compiler.Diagnostics;
using TypedGML.Compiler.Tests.Helpers;

namespace TypedGML.Compiler.Tests.Verification;

public sealed class ControlFlowTests
{
    [Fact] public void Test_BreakOutsideLoop_TGML0033() => AssertHasError(CompileInMethod("break;"), DiagnosticCode.BreakOrContinueOutsideLoopOrSwitch);
    [Fact] public void Test_BreakInsideSwitch_Valid() => AssertValid(CompileInMethod("switch (1) { case 1: break; default: break; }"));
    [Fact] public void Test_ContinueOutsideLoop_TGML0033() => AssertHasError(CompileInMethod("continue;"), DiagnosticCode.BreakOrContinueOutsideLoopOrSwitch);
    [Fact] public void Test_ContinueInsideFor_Valid() => AssertValid(CompileInMethod("for (var i = 0; true; i += 1) { continue; }"));
    [Fact] public void Test_MissingReturnInNonVoidMethod_TGML0034() => AssertHasError(Compile("public class ControlFlowHost { public number Foo() { } }"), DiagnosticCode.MissingReturnInNonVoidMethod);
    [Fact] public void Test_ReturnInAllPaths_Valid() => AssertValid(Compile("public class ControlFlowHost { public number Foo(bool cond) { if (cond) { return 1; } else { return 2; } } }"));
    [Fact] public void Test_ThrowSatisfiesNonVoidReturn_Valid() => AssertValid(Compile("using TypedGML.Core; public class ControlFlowHost { public number Foo() { throw new Exception(\"err\"); } }"));
    [Fact] public void Test_ThrowElseBranchSatisfiesNonVoidReturn_Valid() => AssertValid(Compile("using TypedGML.Core; public class ControlFlowHost { public number Foo(bool x) { if (x) { return 1; } else { throw new Exception(\"err\"); } } }"));
    [Fact] public void Test_StatementAfterThrow_UnreachableWarning() => AssertHasWarning(Compile("using TypedGML.Core; public class ControlFlowHost { public number Foo() { throw new Exception(\"err\"); return 0; } }"), DiagnosticCode.TypeMismatch);
    [Fact] public void Test_ReturnValueInVoidMethod_TGML0034() => AssertHasError(Compile("public class ControlFlowHost { public void Foo() { return 1; } }"), DiagnosticCode.InvalidReturnUsage);
    [Fact] public void Test_SwitchCaseNonConstant_TGML0018() => AssertHasError(CompileInMethod("var label = 1; switch (label) { case label: break; default: break; }"), DiagnosticCode.NonConstantSwitchCaseLabel);
    [Fact] public void Test_SwitchCaseConstant_Valid() => AssertValid(Compile("public class ControlFlowHost { public const number One = 1; public void Run(number value) { switch (value) { case One: break; case 2: break; default: break; } } }"));
    [Fact] public void Test_DuplicateSwitchCase_TGML0020() => AssertHasError(CompileInMethod("switch (1) { case 1: break; case 1: break; default: break; }"), DiagnosticCode.DuplicateSwitchCaseLabel);
    [Fact] public void Test_ThrowNonException_TGML0017() => AssertHasError(Compile("public struct SomeOtherStruct { } public class ControlFlowHost { public void Run() { throw new SomeOtherStruct(); } }"), DiagnosticCode.InvalidThrowType);
    [Fact] public void Test_ThrowException_Valid() => AssertValid(Compile("using TypedGML.Core; public class ControlFlowHost { public void Run() { throw new Exception(\"msg\"); } }"));
    [Fact] public void Test_CatchExceptionMembers_Valid() => AssertValid(Compile("public class ControlFlowHost { public void Run() { try { throw new Exception(\"msg\"); } catch (Exception e) { var a = e.Message; var b = e.message; } } }"));
    [Fact] public void Test_WithOnStruct_TGML0011() => AssertHasError(Compile("public struct SomeStruct { } public class ControlFlowHost { public void Run() { var someStruct = new SomeStruct(); with (someStruct) { } } }"), DiagnosticCode.WithTargetNotObjectType);
    [Fact] public void Test_IfConditionNumber_TGML0019() => AssertHasError(CompileInMethod("number n = 1; if (n) { }"), DiagnosticCode.ImplicitNumberBoolConversion);
    [Fact] public void Test_IfConditionBool_Valid() => AssertValid(CompileInMethod("if (true) { }"));
    [Fact] public void Test_ForConditionNumber_TGML0019() => AssertHasError(CompileInMethod("number i = 1; for (; i; i += 1) { }"), DiagnosticCode.ImplicitNumberBoolConversion);
    [Fact] public void Test_ForConditionComparisonFromInitializer_Valid() => AssertValid(CompileInMethod("for (var i = 0; i < 5; i += 1) { var x = i; }"));
    [Fact] public void Test_WhileConditionComparison_Valid() => AssertValid(CompileInMethod("var i = 0; while (i < 5) { i += 1; }"));
    [Fact] public void Test_RepeatNonNumber_TGML0022() => AssertHasError(CompileInMethod("repeat (true) { }"), DiagnosticCode.TypeMismatch);

    [Fact]
    public void Test_ForConditionComparison_Emits()
    {
        var result = CompileInMethod("for (var i = 0; i < 5; i += 1) { var x = i; }");

        AssertValid(result);
        var gml = result.GetFile("ControlFlowHost.gml")!;
        gml.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Should().Contain("for (var i = 0; i < 5; i += 1) {\n        var x = i;\n    }");
    }

    private static CompileResult CompileInMethod(string statement) =>
        Compile($$"""
            public class ControlFlowHost {
                public void Run() {
                    {{statement}}
                }
            }
            """);

    private static CompileResult Compile(string source) =>
        CompilerFixture.Compile(source);

    private static void AssertValid(CompileResult result) =>
        result.HasErrors.Should().BeFalse(string.Join(Environment.NewLine, result.Errors.Select(error => error.Message)));

    private static void AssertHasError(CompileResult result, DiagnosticCode code) =>
        result.HasError(code).Should().BeTrue(string.Join(Environment.NewLine, result.Errors.Select(error => $"{error.Code}: {error.Message}")));

    private static void AssertHasWarning(CompileResult result, DiagnosticCode code)
    {
        AssertValid(result);
        result.Warnings.Any(warning => warning.Code == code && warning.Message.Contains("unreachable", StringComparison.OrdinalIgnoreCase))
            .Should().BeTrue(string.Join(Environment.NewLine, result.Warnings.Select(warning => $"{warning.Code}: {warning.Message}")));
    }
}
