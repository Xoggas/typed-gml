using FluentAssertions;
using TypedGML.Compiler.Tests.Helpers;

namespace TypedGML.Compiler.Tests.Emission;

public sealed class CastNarrowingEmissionTests
{
    [Fact]
    public void Test_AsCastVar_MethodAndPropertyUseCastType()
    {
        var result = CompilerFixture.Compile("""
            public class BankAccount {
                public virtual void Withdraw(number amount) { }
            }

            public class SavingsAccount : BankAccount {
                private number _withdrawalLimit;

                public number WithdrawalLimit {
                    get { return _withdrawalLimit; }
                }

                public override void Withdraw(number amount) { }
            }

            public class AccountRunner {
                public void Run(BankAccount account) {
                    var savings = account as SavingsAccount;
                    savings.Withdraw(100);
                    var limit = savings.WithdrawalLimit;
                }
            }
            """);

        result.HasErrors.Should().BeFalse();
        var gml = result.GetFile("AccountRunner.gml")!;

        GmlAssert.ContainsPattern(gml, "SavingsAccount_Withdraw(savings, 100);");
        GmlAssert.ContainsPattern(gml, "var limit = SavingsAccount_get_WithdrawalLimit(savings);");
        GmlAssert.NotContainsPattern(gml, "savings.Withdraw(100)");
        GmlAssert.NotContainsPattern(gml, "savings.WithdrawalLimit");
    }
}
