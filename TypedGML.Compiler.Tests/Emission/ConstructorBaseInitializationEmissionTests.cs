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

        GmlAssert.ContainsLine(playerCtor, "var __arg_0 = Stats_create(100, 15, 5, 3);");
        GmlAssert.ContainsLine(playerCtor, "inst.__backing_BaseStats = __arg_0;");
        GmlAssert.ContainsLine(playerCtor, "inst.__backing_MaxHp = __arg_0.MaxHp;");
        GmlAssert.ContainsLine(playerCtor, "Entity_set_CurrentHp(inst, __arg_0.MaxHp);");
        GmlAssert.ContainsLine(playerCtor, "inst.__backing_MoveSpeed = __arg_0.Speed;");
        playerCtor.Split("Stats_create(100, 15, 5, 3)", StringSplitOptions.None).Length.Should().Be(2);
    }

    [Fact]
    public void Test_BaseConstructorComplexArgumentSharedAcrossInlineChain()
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

                public constructor(Stats stats) {
                    BaseStats = stats;
                    MaxHp = stats.MaxHp;
                }
            }

            public class Enemy : Entity {
                public number Attack { get; }

                public constructor(Stats stats) : base(stats) {
                    Attack = stats.Attack;
                }
            }

            public class Boss : Enemy {
                public constructor() : base(new Stats(500, 40, 20, 2)) {
                }
            }
            """);

        result.HasErrors.Should().BeFalse();
        var bossCtor = FunctionBlock(result.GetFile("Boss.gml")!, "function Boss_create");

        bossCtor.Split("var __arg_0 = Stats_create(500, 40, 20, 2);", StringSplitOptions.None).Length.Should().Be(2);
        bossCtor.Split("Stats_create(500, 40, 20, 2)", StringSplitOptions.None).Length.Should().Be(2);
        bossCtor.Split("var __arg_", StringSplitOptions.None).Length.Should().Be(2);
        GmlAssert.ContainsLine(bossCtor, "inst.__backing_BaseStats = __arg_0;");
        GmlAssert.ContainsLine(bossCtor, "inst.__backing_MaxHp = __arg_0.MaxHp;");
        GmlAssert.ContainsLine(bossCtor, "inst.__backing_Attack = __arg_0.Attack;");
    }

    [Fact]
    public void Test_DerivedConstructorSkipsBaseAutoPropertyDefaultsAssignedByChain()
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

        GmlAssert.NotContainsPattern(savingsCtor, "inst.__backing_Owner = \"\";");
        GmlAssert.NotContainsPattern(savingsCtor, "inst.__backing_AccountNumber = \"\";");
        GmlAssert.NotContainsPattern(savingsCtor, "inst.__backing_Balance = 0;");
        GmlAssert.NotContainsPattern(savingsCtor, "inst.__backing_HasTransaction = false;");
        GmlAssert.NotContainsPattern(savingsCtor, "inst.__backing_WithdrawalLimit = 0;");
        GmlAssert.ContainsPattern(savingsCtor, "BankAccount_set_Owner(inst, owner);");
        GmlAssert.ContainsPattern(savingsCtor, "inst.__backing_AccountNumber = accountNumber;");
        GmlAssert.ContainsPattern(savingsCtor, "BankAccount_set_Balance(inst, 0);");
        GmlAssert.ContainsPattern(savingsCtor, "BankAccount_set_HasTransaction(inst, false);");
        GmlAssert.ContainsPattern(savingsCtor, "SavingsAccount_set_WithdrawalLimit(inst, 100);");
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
