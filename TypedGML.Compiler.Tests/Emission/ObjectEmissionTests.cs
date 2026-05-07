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
                public constructor(number x, number y, string layer) : base(x, y, layer) { }
            }
            """).GetFile("Foo.gml")!;
        GmlAssert.HasFunction(gml, "Foo_create");
        GmlAssert.ContainsPattern(gml, "var __inst = instance_create_layer(x, y, layer, OBJ_Foo);");
        GmlAssert.ContainsPattern(gml, "return __inst;");
    }

    [Fact]
    public void Test_ObjectCreation_AlwaysCallsCreateFunction()
    {
        var result = Compile("""
            using TypedGML.GameObjects;

            @Object("obj_Player")
            public class Player : GameObject {
                public constructor(string name) : base(0, 0, "player_layer") { }
            }

            public class Spawner {
                public void Spawn() {
                    var player = new Player("Ada");
                }
            }
            """);

        result.HasErrors.Should().BeFalse(ErrorText(result));
        var player = result.GetFile("Player.gml")!;
        var spawner = result.GetFile("Spawner.gml")!;

        GmlAssert.ContainsPattern(player, "function Player_create(name)");
        GmlAssert.ContainsPattern(player, "var __inst = instance_create_layer(0, 0, \"player_layer\", obj_Player);");
        GmlAssert.ContainsPattern(spawner, "var player = Player_create(\"Ada\");");
        GmlAssert.NotContainsPattern(spawner, "instance_create_layer");
    }

    [Fact]
    public void Test_ObjectCreate_TracesForwardedBaseChain()
    {
        var result = Compile("""
            using TypedGML.GameObjects;

            @Object("obj_Actor")
            public class Actor : GameObject {
                public constructor(number x, number y, string layer) : base(x, y, layer) { }
            }

            @Object("obj_Player")
            public class Player : Actor {
                public constructor(number px, number py, string targetLayer, number hp) : base(px, py, targetLayer) { }
            }
            """);

        result.HasErrors.Should().BeFalse(ErrorText(result));
        var player = result.GetFile("Player.gml")!;

        GmlAssert.ContainsPattern(player, "function Player_create(px, py, targetLayer, hp)");
        GmlAssert.ContainsPattern(player, "var __inst = instance_create_layer(px, py, targetLayer, obj_Player);");
    }

    [Fact]
    public void Test_ObjectCreate_ConstructorBodyRunsInsideInstanceBlock()
    {
        var result = Compile("""
            using TypedGML.GameObjects;

            @Object("obj_Player")
            public class Player : GameObject {
                public string Name { get; set; }

                public constructor(string name) : base(0, 0, "player_layer") {
                    Name = name;
                }
            }
            """);

        result.HasErrors.Should().BeFalse(ErrorText(result));
        var player = result.GetFile("Player.gml")!;

        GmlAssert.ContainsPattern(player, "with (__inst) {");
        GmlAssert.ContainsPattern(player, "Player_set_Name(self, name);");
    }

    [Fact]
    public void Test_ObjectCreateWithExtraParams()
    {
        var gml = Compile("""
            using TypedGML.GameObjects;
            @Object("OBJ_Player")
            public class Player : GameObject {
                public number HitPoints;
                public constructor(number x, number y, string layer, number health) : base(x, y, layer) { HitPoints = health; }
            }
            """).GetFile("Player.gml")!;
        GmlAssert.ContainsPattern(gml, "with (__inst) {");
        GmlAssert.ContainsPattern(gml, "HitPoints = health;");
        GmlAssert.NotContainsPattern(Normalize(gml), "with (__inst)\n    {");
    }

    [Fact]
    public void Test_ObjectCreateWithExtraParamWithoutBodyDoesNotAssignMatchingField()
    {
        var gml = Compile("""
            using TypedGML.GameObjects;
            @Object("OBJ_Enemy")
            public class Enemy : GameObject {
                public number Hp;
                public constructor(number x, number y, string layer, number hp) : base(x, y, layer) { }
            }
            """).GetFile("Enemy.gml")!;

        var normalized = Normalize(gml);
        GmlAssert.NotContainsPattern(normalized, "with (__inst)");
        GmlAssert.NotContainsPattern(normalized, "Hp = hp;");
    }

    [Fact]
    public void Test_ObjectCreateWithExtraParamsWithoutBodyDoesNotAssignMatchingFields()
    {
        var gml = Compile("""
            using TypedGML.GameObjects;
            @Object("OBJ_Enemy")
            public class Enemy : GameObject {
                public number Hp;
                public number MoveSpeed;
                public constructor(number x, number y, string layer, number hp, number moveSpeed) : base(x, y, layer) { }
            }
            """).GetFile("Enemy.gml")!;

        var normalized = Normalize(gml);
        GmlAssert.NotContainsPattern(normalized, "with (__inst)");
        GmlAssert.NotContainsPattern(normalized, "Hp = hp;");
        GmlAssert.NotContainsPattern(normalized, "MoveSpeed = moveSpeed;");
    }

    [Fact]
    public void Test_ObjectEventFile_OnlyOverridden()
    {
        var result = Compile("""
            using TypedGML.GameObjects;
            @Object("OBJ_Player")
            public class Player : GameObject {
                public constructor(number x, number y, string layer) : base(x, y, layer) { }
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
                public constructor(number x, number y, string layer) : base(x, y, layer) { }
            }
            """).GetFile("OBJ_Player/Create_0.gml")!;

        gml.Should().NotBeNull();
        GmlAssert.ContainsLine(gml, "self.Positions = [0];");
        Normalize(gml).TrimStart().Should().StartWith("self.Positions = [0];");
    }

    [Fact]
    public void Test_ObjectCreateEvent_InitializesInstanceStateBeforeOnCreate()
    {
        var gml = Compile("""
            using TypedGML.GameObjects;

            public delegate void DeathHandler();

            @Object("OBJ_Player")
            public class Player : GameObject {
                public number Timer;
                public string Name { get; set; }
                public event DeathHandler OnDeath;

                public constructor(number x, number y, string layer) : base(x, y, layer) { }

                public override void OnCreate() {
                    Timer = Timer + 1;
                }
            }
            """).GetFile("OBJ_Player/Create_0.gml")!;

        var normalized = Normalize(gml);
        GmlAssert.ContainsLine(gml, "self.Timer = 0;");
        GmlAssert.ContainsLine(gml, "self.__backing_Name = undefined;");
        GmlAssert.ContainsLine(gml, "self.__event_OnDeath = [];");
        normalized.IndexOf("self.__event_OnDeath = [];", StringComparison.Ordinal)
            .Should().BeLessThan(normalized.IndexOf("Timer = Timer + 1;", StringComparison.Ordinal));
    }

    [Fact]
    public void Test_NativePropertyInEvent()
    {
        var gml = Compile("""
            using TypedGML.GameObjects;
            @Object("OBJ_Player")
            public class Player : GameObject {
                public constructor(number x, number y, string layer) : base(x, y, layer) { }
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
            public class Enemy : GameObject {
                public constructor(number x, number y, string layer) : base(x, y, layer) { }
            }

            @Object("obj_Player")
            public class Player : GameObject {
                public number Hits;
                public constructor(number x, number y, string layer) : base(x, y, layer) { }

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

    private static string ErrorText(CompileResult result) =>
        string.Join(Environment.NewLine, result.Errors.Select(error => $"{error.Code}: {error.Message}"));
}
