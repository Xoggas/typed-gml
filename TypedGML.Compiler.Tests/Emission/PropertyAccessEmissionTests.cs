using FluentAssertions;
using TypedGML.Compiler.Tests.Helpers;

namespace TypedGML.Compiler.Tests.Emission;

public sealed class PropertyAccessEmissionTests
{
    [Fact]
    public void Test_InstancePropertyReadsUseGetters()
    {
        var result = Compile("""
            public class BankAccount {
                private number _balance;
                private string _owner;

                public number Balance {
                    get { return _balance; }
                    set { _balance = value; }
                }

                public string Owner {
                    get { return _owner; }
                }
            }

            public class SavingsAccount : BankAccount {
                private number _withdrawalLimit;

                public number WithdrawalLimit {
                    get { return _withdrawalLimit; }
                }
            }

            public class AccountReader {
                public number ReadBalance(BankAccount account) { return account.Balance; }
                public string ReadOwner(BankAccount a) { return a.Owner; }
                public number ReadLimit(SavingsAccount savings) { return savings.WithdrawalLimit; }
            }
            """);

        result.HasErrors.Should().BeFalse();
        var gml = result.GetFile("AccountReader.gml")!;

        GmlAssert.ContainsPattern(gml, "return BankAccount_get_Balance(account);");
        GmlAssert.ContainsPattern(gml, "return BankAccount_get_Owner(a);");
        GmlAssert.ContainsPattern(gml, "return SavingsAccount_get_WithdrawalLimit(savings);");
        GmlAssert.NotContainsPattern(gml, "account.Balance");
        GmlAssert.NotContainsPattern(gml, "a.Owner");
        GmlAssert.NotContainsPattern(gml, "savings.WithdrawalLimit");
    }

    [Fact]
    public void Test_GenericBclPropertyReadUsesGetter()
    {
        var result = Compile("""
            using TypedGML.Collections;

            public class BankAccount {
            }

            public class AccountHolder {
                private List<BankAccount> _accounts;

                public number CountAccounts() {
                    return this._accounts.Count;
                }
            }
            """);

        result.HasErrors.Should().BeFalse();
        var gml = result.GetFile("AccountHolder.gml")!;

        GmlAssert.ContainsPattern(gml, "return TypedGML_Collections_List1_get_Count(inst._accounts);");
        GmlAssert.NotContainsPattern(gml, "inst._accounts.Count");
    }

    [Fact]
    public void Test_InferredListIndexerVariablePropertyReadsUseGetters()
    {
        var result = Compile("""
            using TypedGML.Collections;

            public class BankAccount {
                private string _accountNumber;
                private string _owner;
                private number _balance;

                public string AccountNumber { get { return _accountNumber; } }
                public string Owner { get { return _owner; } }
                public number Balance { get { return _balance; } }
            }

            public class Bank {
                private List<BankAccount> _accounts;

                public void ListAccounts() {
                    var a = this._accounts[0];
                    var accountNumber = a.AccountNumber;
                    var owner = a.Owner;
                    var balance = a.Balance;
                }
            }
            """);

        result.HasErrors.Should().BeFalse();
        var gml = result.GetFile("Bank.gml")!;

        GmlAssert.ContainsPattern(gml, "var accountNumber = BankAccount_get_AccountNumber(a);");
        GmlAssert.ContainsPattern(gml, "var owner = BankAccount_get_Owner(a);");
        GmlAssert.ContainsPattern(gml, "var balance = BankAccount_get_Balance(a);");
        GmlAssert.NotContainsPattern(gml, "a.AccountNumber");
        GmlAssert.NotContainsPattern(gml, "a.Owner");
        GmlAssert.NotContainsPattern(gml, "a.Balance");
    }

    [Fact]
    public void Test_InheritedPropertyReadFromAsCastVariableUsesDeclaringGetter()
    {
        var result = Compile("""
            public class Account {
            }

            public class BankAccount : Account {
                private number _balance;

                public number Balance { get { return _balance; } }
            }

            public class SavingsAccount : BankAccount {
            }

            public class Bank {
                public void WithdrawSavings(Account account) {
                    var savings = account as SavingsAccount;
                    var balance = savings.Balance;
                }
            }
            """);

        result.HasErrors.Should().BeFalse();
        var gml = result.GetFile("Bank.gml")!;

        GmlAssert.ContainsPattern(gml, "var balance = BankAccount_get_Balance(savings);");
        GmlAssert.NotContainsPattern(gml, "savings.Balance");
    }

    [Fact]
    public void Test_InstancePropertyWritesUseSetters()
    {
        var result = Compile("""
            public class BankAccount {
                private number _balance;

                public number Balance {
                    get { return _balance; }
                    set { _balance = value; }
                }
            }

            public class AccountWriter {
                public void Write(BankAccount account, number value) {
                    account.Balance = value;
                    account.Balance += 1;
                }
            }
            """);

        result.HasErrors.Should().BeFalse();
        var gml = result.GetFile("AccountWriter.gml")!;

        GmlAssert.ContainsPattern(gml, "BankAccount_set_Balance(account, value);");
        GmlAssert.ContainsPattern(gml, "BankAccount_set_Balance(account, BankAccount_get_Balance(account) + 1);");
        GmlAssert.NotContainsPattern(gml, "account.Balance");
    }

    private static CompileResult Compile(string source) => CompilerFixture.Compile(source);
}
