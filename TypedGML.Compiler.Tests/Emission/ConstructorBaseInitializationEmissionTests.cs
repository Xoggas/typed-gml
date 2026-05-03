using FluentAssertions;
using TypedGML.Compiler.Tests.Helpers;

namespace TypedGML.Compiler.Tests.Emission;

public sealed class ConstructorBaseInitializationEmissionTests
{
    [Fact]
    public void Test_BaseConstructorComplexArgumentIsEvaluatedOnce()
    {
        var result = CompilerFixture.Compile("""
            public class Stats {
                public number MaxHp;
                public number Attack;
                public number Defense;
                public number Speed;

                public constructor(number maxHp, number attack, number defense, number speed) {
                    MaxHp = maxHp;
                    Attack = attack;
                    Defense = defense;
                    Speed = speed;
                }
            }

            public class Entity {
                public Stats BaseStats { get; }
                public number MaxHp { get; }
                public number MoveSpeed { get; }
                public number CurrentHp { get; set; }

                public constructor(number x, number y, string layer, Stats stats) {
                    BaseStats = stats;
                    MaxHp = stats.MaxHp;
                    CurrentHp = stats.MaxHp;
                    MoveSpeed = stats.Speed;
                }
            }

            public class Player : Entity {
                public constructor(number x, number y, string layer) : base(x, y, layer, new Stats(100, 15, 5, 3)) {
                }
            }
            """);

        result.HasErrors.Should().BeFalse();
        var playerCtor = FunctionBlock(result.GetFile("Player.gml")!, "function Player_create");

        GmlAssert.ContainsLine(playerCtor, "var __arg_stats = Stats_create(100, 15, 5, 3);");
        GmlAssert.ContainsLine(playerCtor, "self.__backing_BaseStats = __arg_stats;");
        GmlAssert.ContainsLine(playerCtor, "self.__backing_MaxHp = __arg_stats.MaxHp;");
        GmlAssert.ContainsLine(playerCtor, "Entity_set_CurrentHp(self, __arg_stats.MaxHp);");
        GmlAssert.ContainsLine(playerCtor, "self.__backing_MoveSpeed = __arg_stats.Speed;");
        playerCtor.Split("Stats_create(100, 15, 5, 3)", StringSplitOptions.None).Length.Should().Be(2);
    }

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
