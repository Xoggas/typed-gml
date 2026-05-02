using FluentAssertions;
using TypedGML.Compiler.Tests.Helpers;

namespace TypedGML.Compiler.Tests.Emission;

public sealed class ConstructorBaseInitializationEmissionTests
{
    [Fact]
    public void Test_DerivedConstructorEmitsBaseAutoPropertyDefaultsBeforeBaseBody()
    {
        var result = CompilerFixture.Compile("""
            public class BankAccount {
                public string Owner { get; set; }
                public string AccountNumber { get; }
                public number Balance { get; set; }
                public bool HasTransaction { get; set; }

                public constructor(string owner, string accountNumber) {
                    Owner = owner;
                    AccountNumber = accountNumber;
                    Balance = 0;
                    HasTransaction = false;
                }
            }

            public class SavingsAccount : BankAccount {
                public number WithdrawalLimit { get; set; }

                public constructor(string owner, string accountNumber) : base(owner, accountNumber) {
                    WithdrawalLimit = 100;
                }
            }
            """);

        result.HasErrors.Should().BeFalse();
        var savingsCtor = FunctionBlock(result.GetFile("SavingsAccount.gml")!, "function SavingsAccount_create");

        IndexOf(savingsCtor, "self.__backing_Owner = undefined;").Should().BeLessThan(IndexOf(savingsCtor, "BankAccount_set_Owner(self, owner);"));
        IndexOf(savingsCtor, "self.__backing_AccountNumber = undefined;").Should().BeLessThan(IndexOf(savingsCtor, "BankAccount_set_Owner(self, owner);"));
        IndexOf(savingsCtor, "self.__backing_Balance = 0;").Should().BeLessThan(IndexOf(savingsCtor, "BankAccount_set_Owner(self, owner);"));
        IndexOf(savingsCtor, "self.__backing_HasTransaction = false;").Should().BeLessThan(IndexOf(savingsCtor, "BankAccount_set_Owner(self, owner);"));
        IndexOf(savingsCtor, "self.__backing_WithdrawalLimit = 0;").Should().BeGreaterThan(IndexOf(savingsCtor, "BankAccount_set_Owner(self, owner);"));
    }

    private static string FunctionBlock(string gml, string prefix)
    {
        var start = gml.IndexOf(prefix, StringComparison.Ordinal);
        start.Should().BeGreaterThan(-1);
        var next = gml.IndexOf("\n\nfunction ", start + prefix.Length, StringComparison.Ordinal);
        return next < 0 ? gml[start..] : gml[start..next];
    }

    private static int IndexOf(string text, string value)
    {
        var index = text.IndexOf(value, StringComparison.Ordinal);
        index.Should().BeGreaterThan(-1);
        return index;
    }
}
