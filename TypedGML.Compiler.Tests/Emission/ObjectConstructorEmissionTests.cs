using FluentAssertions;
using TypedGML.Compiler.Tests.Helpers;

namespace TypedGML.Compiler.Tests.Emission;

public sealed class ObjectConstructorEmissionTests
{
    [Fact]
    public void Test_ObjectConstructor_DoesNotEmitParameterSelfAssignments()
    {
        var result = CompilerFixture.Compile("""
            using TypedGML.GameObjects;

            public struct Stats {
                public number Health;

                public constructor(number health) {
                    Health = health;
                }
            }

            @Object("obj_Entity")
            public class Entity : GameObject {
                public Stats BaseStats;

                public constructor(number x, number y, string layer, Stats stats) : base(x, y, layer) {
                    BaseStats = stats;
                }
            }

            @Object("obj_Enemy")
            public class Enemy : Entity {
                public constructor(number x, number y, string layer) : base(x, y, layer, new Stats(30)) {
                }
            }
            """);

        result.HasErrors.Should().BeFalse(Errors(result));
        var entity = result.GetFile("Entity.gml")!;
        var enemy = result.GetFile("Enemy.gml")!;

        GmlAssert.NotContainsPattern(entity, "stats = stats;");
        GmlAssert.NotContainsPattern(enemy, "layer = layer;");
        GmlAssert.NotContainsPattern(enemy, "x = x;");
        GmlAssert.NotContainsPattern(enemy, "y = y;");
    }

    private static string Errors(CompileResult result) =>
        string.Join(Environment.NewLine, result.Errors.Select(error => $"{error.Code}: {error.Message}"));
}
