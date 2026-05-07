using FluentAssertions;
using TypedGML.Compiler.Tests.Helpers;

namespace TypedGML.Compiler.Tests.Emission;

public sealed class ObjectCreationCallSiteEmissionTests
{
    [Fact]
    public void Test_ObjectCreationInStaticMethod_AlwaysCallsCreateFunction()
    {
        var result = CompilerFixture.Compile("""
            using TypedGML.GameObjects;

            @Object("obj_Player")
            public class Player : GameObject {
                public constructor(string name) : base(0, 0, "player_layer") { }
            }

            @Object("obj_Enemy")
            public class Enemy : GameObject {
                public constructor(number x, number y, string layer) : base(x, y, layer) { }
            }

            @Object("obj_Goblin")
            public class Goblin : GameObject {
                public constructor(number x, number y, string layer) : base(x, y, layer) { }
            }

            public class Factory {
                public static void SpawnAll(number x, number y, string layer) {
                    var player = new Player("Alice");
                    var enemy = new Enemy(x, y, layer);
                    var goblin = new Goblin(x, y, layer);
                }
            }
            """);

        result.HasErrors.Should().BeFalse(Errors(result));
        var factory = result.GetFile("Factory.gml")!;

        GmlAssert.ContainsPattern(factory, """var player = Player_create("Alice");""");
        GmlAssert.ContainsPattern(factory, "var enemy = Enemy_create(x, y, layer);");
        GmlAssert.ContainsPattern(factory, "var goblin = Goblin_create(x, y, layer);");
        GmlAssert.NotContainsPattern(factory, "instance_create_layer");
        Count(result.AllOutput(), "instance_create_layer").Should().Be(3);
        GmlAssert.ContainsPattern(FunctionBlock(result.GetFile("Player.gml")!, "function Player_create"), "instance_create_layer");
        GmlAssert.ContainsPattern(FunctionBlock(result.GetFile("Enemy.gml")!, "function Enemy_create"), "instance_create_layer");
        GmlAssert.ContainsPattern(FunctionBlock(result.GetFile("Goblin.gml")!, "function Goblin_create"), "instance_create_layer");
    }

    private static string FunctionBlock(string gml, string prefix)
    {
        var start = gml.IndexOf(prefix, StringComparison.Ordinal);
        start.Should().BeGreaterThan(-1);
        var next = gml.IndexOf("\n\nfunction ", start + prefix.Length, StringComparison.Ordinal);
        return next < 0 ? gml[start..] : gml[start..next];
    }

    private static int Count(string value, string pattern) =>
        value.Split(pattern, StringSplitOptions.None).Length - 1;

    private static string Errors(CompileResult result) =>
        string.Join(Environment.NewLine, result.Errors.Select(error => $"{error.Code}: {error.Message}"));
}
