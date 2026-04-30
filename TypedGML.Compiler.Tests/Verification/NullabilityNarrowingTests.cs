using FluentAssertions;
using TypedGML.Compiler.Diagnostics;
using TypedGML.Compiler.Tests.Helpers;

namespace TypedGML.Compiler.Tests.Verification;

public sealed class NullabilityNarrowingTests
{
    [Fact]
    public void Test_NullNarrowing_AfterNullCheckReturn() =>
        AssertNoNullabilityError(CompileRun("""
            var x = MaybeNull();
            if (x == null) {
                return;
            }
            x.Method();
            """));

    [Fact]
    public void Test_NullNarrowing_AfterNullCheckThrow() =>
        AssertNoNullabilityError(CompileRun("""
            var x = MaybeNull();
            if (x == null) {
                throw new Exception("missing");
            }
            x.Method();
            """));

    [Fact]
    public void Test_NullNarrowing_IfNotNull_InBranch() =>
        AssertNoNullabilityError(CompileRun("""
            var x = MaybeNull();
            if (x != null) {
                x.Method();
            }
            """));

    [Fact]
    public void Test_NullNarrowing_AfterCompoundNullCheckReturn() =>
        AssertNoNullabilityError(CompileRun("""
            var x = MaybeNull();
            var y = MaybeNull();
            if (x == null or y == null) {
                return;
            }
            x.Method();
            y.Method();
            """));

    [Fact]
    public void Test_NullNarrowing_StillNullableOutsideBranch() =>
        AssertHasNullabilityError(CompileRun("""
            var x = MaybeNull();
            if (x != null) {
            }
            x.Method();
            """));

    [Fact]
    public void Test_TernaryNarrowing_NotNull_ThenBranch() =>
        AssertNoNullabilityError(CompileRun("""
            var x = MaybeNull();
            var label = (x != null) ? x.Field : "default";
            """));

    [Fact]
    public void Test_TernaryNarrowing_IsNull_ElseBranch() =>
        AssertNoNullabilityError(CompileRun("""
            var x = MaybeNull();
            var label = (x == null) ? "fallback" : x.Field;
            """));

    [Fact]
    public void Test_TernaryNarrowing_StillNullableInOppositeBranch() =>
        AssertHasNullabilityError(CompileRun("""
            var x = MaybeNull();
            var label = (x != null) ? "ok" : x.Field;
            """));

    private static CompileResult CompileRun(string statements) =>
        CompilerFixture.Compile($$"""
            using TypedGML.Core;

            public class NarrowTarget {
                public string Field;

                public void Method() {
                }
            }

            public class NarrowHost {
                public NarrowTarget? MaybeNull() {
                    return null;
                }

                public void Run() {
                    {{statements}}
                }
            }
            """);

    private static void AssertNoNullabilityError(CompileResult result) =>
        result.HasError(DiagnosticCode.NullAssignedToNonNullableType).Should().BeFalse(Errors(result));

    private static void AssertHasNullabilityError(CompileResult result) =>
        result.HasError(DiagnosticCode.NullAssignedToNonNullableType).Should().BeTrue(Errors(result));

    private static string Errors(CompileResult result) =>
        string.Join(Environment.NewLine, result.Errors.Select(error => $"{error.Code}: {error.Message}"));
}
