using FluentAssertions;
using TypedGML.Compiler.Diagnostics;
using TypedGML.Compiler.Tests.Helpers;

namespace TypedGML.Compiler.Tests.Verification;

public sealed class ConstructorCallTests
{
    [Fact]
    public void Test_BaseConstructorChain_ReordersNamedArguments()
    {
        var result = CompilerFixture.Compile("""
            public struct Stats {
                public constructor(number health, number attack, number defense, number speed) { }
            }

            public class Actor {
                public number AggroRange;
                public number AttackRange;
                public number ExperienceReward;

                public constructor(number x, number y, string layer, Stats stats, number aggroRange, number attackRange, number experienceReward) {
                    AggroRange = aggroRange;
                    AttackRange = attackRange;
                    ExperienceReward = experienceReward;
                }
            }

            public class Enemy : Actor {
                public constructor(number x, number y, string layer) : base(
                    x, y, layer, new Stats(30, 8, 2, 4),
                    aggroRange: 150,
                    attackRange: 24,
                    experienceReward: 20
                ) { }
            }
            """);

        result.HasError(DiagnosticCode.NoMatchingMethodOverload).Should().BeFalse(Errors(result));
        result.HasErrors.Should().BeFalse(Errors(result));
        GmlAssert.ContainsPattern(result.GetFile("/Enemy.gml")!, "self.AggroRange = 150;");
    }

    private static string Errors(CompileResult result) =>
        string.Join(Environment.NewLine, result.Errors.Select(error => $"{error.Code}: {error.Message}"));
}
