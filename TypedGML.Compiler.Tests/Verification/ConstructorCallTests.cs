using FluentAssertions;
using TypedGML.Compiler.Diagnostics;
using TypedGML.Compiler.Tests.Helpers;

namespace TypedGML.Compiler.Tests.Verification;

public sealed class ConstructorCallTests
{
    [Fact]
    public void Test_DerivedConstructor_ImplicitBaseParameterlessAccepted()
    {
        var result = CompilerFixture.Compile("""
            public class Parent {
                public number X;
                public constructor() {
                    X = 1;
                }
            }

            public class Child : Parent {
                public constructor() { }
            }
            """);

        result.HasErrors.Should().BeFalse(Errors(result));
        GmlAssert.ContainsPattern(result.GetFile("/Child.gml")!, "inst.X = 1;");
    }

    [Fact]
    public void Test_DerivedClassWithoutConstructor_RequiresBaseParameterless()
    {
        var result = CompilerFixture.Compile("""
            public class Parent {
                public constructor(number x) { }
            }

            public class Child : Parent {
            }
            """);

        result.HasError(DiagnosticCode.NoMatchingMethodOverload).Should().BeTrue(Errors(result));
        result.Errors.Single(error => error.Code == DiagnosticCode.NoMatchingMethodOverload)
            .Message.Should().Be("Constructor does not chain to a matching base constructor.");
    }

    [Fact]
    public void Test_DerivedConstructorWithoutBase_RequiresBaseParameterless()
    {
        var result = CompilerFixture.Compile("""
            public class Parent {
                public constructor(number x) { }
            }

            public class Child : Parent {
                public constructor() { }
            }
            """);

        result.HasError(DiagnosticCode.NoMatchingMethodOverload).Should().BeTrue(Errors(result));
        result.Errors.Single(error => error.Code == DiagnosticCode.NoMatchingMethodOverload)
            .Message.Should().Be("Constructor does not chain to a matching base constructor.");
    }

    [Fact]
    public void Test_DerivedConstructorBaseArgs_MustMatchParentConstructor()
    {
        var result = CompilerFixture.Compile("""
            public class Parent {
                public constructor(number x) { }
            }

            public class Child : Parent {
                public constructor(string x) : base(x) { }
            }
            """);

        result.HasError(DiagnosticCode.NoMatchingMethodOverload).Should().BeTrue(Errors(result));
        result.Errors.Single(error => error.Code == DiagnosticCode.NoMatchingMethodOverload)
            .Message.Should().Be("Constructor does not chain to a matching base constructor.");
    }

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
        GmlAssert.ContainsPattern(result.GetFile("/Enemy.gml")!, "inst.AggroRange = 150;");
    }

    private static string Errors(CompileResult result) =>
        string.Join(Environment.NewLine, result.Errors.Select(error => $"{error.Code}: {error.Message}"));
}
