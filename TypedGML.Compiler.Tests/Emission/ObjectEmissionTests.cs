using FluentAssertions;
using TypedGML.Compiler.Tests.Helpers;

namespace TypedGML.Compiler.Tests.Emission;

public sealed class ObjectEmissionTests
{
    [Fact]
    public void Test_ObjectCreateFunction()
    {
        var gml = Compile("""
            using TypedGML.GameObjects;
            @Object("OBJ_Foo")
            public class Foo : GameObject {
                public constructor(number x, number y, string layer) { }
            }
            """).GetFile("Foo.gml")!;
        GmlAssert.HasFunction(gml, "Foo_create");
        GmlAssert.ContainsPattern(gml, "return instance_create_layer(x, y, layer, OBJ_Foo);");
    }

    [Fact]
    public void Test_ObjectCreateWithExtraParams()
    {
        var gml = Compile("""
            using TypedGML.GameObjects;
            @Object("OBJ_Player")
            public class Player : GameObject {
                public number Health;
                public constructor(number x, number y, string layer, number health) { Health = health; }
            }
            """).GetFile("Player.gml")!;
        GmlAssert.ContainsPattern(gml, "with (__inst) {");
        GmlAssert.ContainsPattern(gml, "Health = health;");
        GmlAssert.NotContainsPattern(Normalize(gml), "with (__inst)\n    {");
    }

    [Fact]
    public void Test_ObjectCreateWithOneExtraParamAssignsInsideFlatWithBlock()
    {
        var gml = Compile("""
            using TypedGML.GameObjects;
            @Object("OBJ_Enemy")
            public class Enemy : GameObject {
                public number Hp;
                public constructor(number x, number y, string layer, number hp) { }
            }
            """).GetFile("Enemy.gml")!;

        var normalized = Normalize(gml);
        GmlAssert.ContainsPattern(normalized, "with (__inst) {\n        Hp = hp;\n    }");
        GmlAssert.NotContainsPattern(normalized, "with (__inst)\n    {");
    }

    [Fact]
    public void Test_ObjectCreateWithTwoExtraParamsAssignsInsideFlatWithBlock()
    {
        var gml = Compile("""
            using TypedGML.GameObjects;
            @Object("OBJ_Enemy")
            public class Enemy : GameObject {
                public number Hp;
                public number Speed;
                public constructor(number x, number y, string layer, number hp, number speed) { }
            }
            """).GetFile("Enemy.gml")!;

        var normalized = Normalize(gml);
        GmlAssert.ContainsPattern(normalized, "with (__inst) {\n        Hp = hp;\n        Speed = speed;\n    }");
        GmlAssert.NotContainsPattern(normalized, "with (__inst)\n    {");
    }

    [Fact]
    public void Test_ObjectEventFile_OnlyOverridden()
    {
        var result = Compile("""
            using TypedGML.GameObjects;
            @Object("OBJ_Player")
            public class Player : GameObject {
                public override void OnCreate() { }
                public override void OnStep() { }
            }
            """);
        result.GetFile("OBJ_Player/Create_0.gml").Should().NotBeNull();
        result.GetFile("OBJ_Player/Step_0.gml").Should().NotBeNull();
        result.GetFile("OBJ_Player/Draw_0.gml").Should().BeNull();
        result.GetFile("OBJ_Player/Alarm_0.gml").Should().BeNull();
    }

    [Fact]
    public void Test_ObjectClass_FieldInitializerInCreateEvent()
    {
        var gml = Compile("""
            using TypedGML.GameObjects;
            @Object("OBJ_Player")
            public class Player : GameObject {
                public number[] Positions = [0];
                public constructor(number x, number y, string layer) { }
            }
            """).GetFile("OBJ_Player/Create_0.gml")!;

        gml.Should().NotBeNull();
        GmlAssert.ContainsLine(gml, "Positions = [0];");
        Normalize(gml).TrimStart().Should().StartWith("Positions = [0];");
    }

    [Fact]
    public void Test_NativePropertyInEvent()
    {
        var gml = Compile("""
            using TypedGML.GameObjects;
            @Object("OBJ_Player")
            public class Player : GameObject {
                public override void OnStep() { X = X + 1; }
            }
            """).GetFile("OBJ_Player/Step_0.gml")!;
        GmlAssert.ContainsPattern(gml, "x =");
        GmlAssert.ContainsPattern(gml, "x + 1");
        GmlAssert.NotContainsPattern(gml, "self.X");
    }

    [Fact]
    public void Test_CollisionEventFile_UsesTargetObjectAsset()
    {
        var result = Compile("""
            using TypedGML.GameObjects;
            @Object("obj_Enemy")
            public class Enemy : GameObject { }

            @Object("obj_Player")
            public class Player : GameObject {
                public number Hits;

                @Collision(typeof(Enemy))
                public void OnCollisionEnemy() {
                    Hits = Hits + 1;
                }
            }
            """);

        var gml = result.GetFile("obj_Player/Collision_obj_Enemy.gml")!;
        gml.Should().NotBeNull();
        GmlAssert.ContainsPattern(gml, "Hits = Hits + 1;");
        result.GetFile("obj_Player/CollisionPrefix.gml").Should().BeNull();
    }

    [Fact]
    public void Test_ObjectConstructorChainAcceptsStructCreationArgument()
    {
        var result = Compile("""
            using TypedGML.GameObjects;

            public struct Stats {
                public number Health;
                public number Attack;
                public number Defense;
                public number Speed;

                public constructor(number health, number attack, number defense, number speed) {
                    Health = health;
                    Attack = attack;
                    Defense = defense;
                    Speed = speed;
                }
            }

            @Object("obj_Actor")
            public class Actor : GameObject {
                public Stats Stats;

                public constructor(number x, number y, string layer, Stats stats) : base(x, y, layer) {
                    Stats = stats;
                }
            }

            @Object("obj_Player")
            public class Player : Actor {
                public constructor(number x, number y, string layer) : base(x, y, layer, new Stats(100, 15, 5, 3)) {
                }
            }
            """);

        result.HasErrors.Should().BeFalse(string.Join(Environment.NewLine, result.Errors.Select(error => $"{error.Code}: {error.Message}")));
        GmlAssert.ContainsPattern(result.GetFile("Player.gml")!, "Stats_create(100, 15, 5, 3)");
    }

    private static CompileResult Compile(string source) => CompilerFixture.Compile(source);

    private static string Normalize(string text) =>
        text.Replace("\r\n", "\n", StringComparison.Ordinal);
}
