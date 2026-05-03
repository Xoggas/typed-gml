using FluentAssertions;
using TypedGML.Compiler.Diagnostics;
using TypedGML.Compiler.Tests.Helpers;

namespace TypedGML.Compiler.Tests.Verification;

public sealed class MethodCallResolutionTests
{
    [Fact]
    public void Test_OverrideNotDuplicatedInCandidates()
    {
        var result = Compile("""
            public class BankAccount {
                public virtual void Withdraw(number amount) { }
            }
            public class SavingsAccount : BankAccount {
                public override void Withdraw(number amount) { }
            }
            public class VerificationHost {
                public void Run(BankAccount account) {
                    var savings = account as SavingsAccount;
                    savings.Withdraw(100);
                }
            }
            """);

        result.HasError(DiagnosticCode.AmbiguousMethodCall).Should().BeFalse(Errors(result));
        result.HasErrors.Should().BeFalse(Errors(result));
    }

    [Fact]
    public void Test_VirtualCallOnBaseRef_UsesOverride()
    {
        var result = Compile("""
            public class Account {
                public virtual void Withdraw(number amount) { }
            }
            public class BankAccount : Account {
                public override void Withdraw(number amount) { }
            }
            public class SavingsAccount : BankAccount {
                public override void Withdraw(number amount) { }
                public void Run(SavingsAccount account) {
                    var bank = account as BankAccount;
                    bank.Withdraw(100);
                }
            }
            """);

        result.HasError(DiagnosticCode.AmbiguousMethodCall).Should().BeFalse(Errors(result));
        result.HasErrors.Should().BeFalse(Errors(result));
    }

    [Fact]
    public void Test_CurrentTypeCall_UsesMostDerivedOverride()
    {
        var result = Compile("""
            public class BankAccount {
                public virtual void Withdraw(number amount) { }
            }
            public class SavingsAccount : BankAccount {
                public override void Withdraw(number amount) { }
                public void Run() {
                    Withdraw(100);
                }
            }
            """);

        result.HasError(DiagnosticCode.AmbiguousMethodCall).Should().BeFalse(Errors(result));
        result.HasErrors.Should().BeFalse(Errors(result));
    }

    [Fact]
    public void Test_TrueOverloads_StillWork()
    {
        var result = Compile("""
            public class T {
                public void Foo(number value) { }
                public void Foo(string value) { }
                public void Run(T t) {
                    t.Foo(1);
                    t.Foo("hi");
                }
            }
            """);

        result.HasErrors.Should().BeFalse(Errors(result));
        var gml = result.GetFile("/T.gml")!;
        GmlAssert.ContainsPattern(gml, "T_Foo__number(t, 1);");
        GmlAssert.ContainsPattern(gml, """T_Foo__string(t, "hi");""");
    }

    private static CompileResult Compile(string source) =>
        CompilerFixture.Compile(source);

    private static string Errors(CompileResult result) =>
        string.Join(Environment.NewLine, result.Errors.Select(error => $"{error.Code}: {error.Message}"));
}
