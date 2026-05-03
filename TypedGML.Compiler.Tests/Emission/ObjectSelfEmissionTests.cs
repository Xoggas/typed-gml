using FluentAssertions;
using TypedGML.Compiler.Tests.Helpers;

namespace TypedGML.Compiler.Tests.Emission;

public sealed class ObjectSelfEmissionTests
{
    [Fact]
    public void Test_ObjectClassMethod_HasSelfParameter()
    {
        var result = Compile("""
            using TypedGML.GameObjects;
            @Object("obj_Entity")
            public class Entity : GameObject {
                public number CurrentHp { get; set; }

                public void TakeDamage(number amount) {
                    CurrentHp = CurrentHp - amount;
                }
            }
            """);

        result.HasErrors.Should().BeFalse(ErrorSummary(result));
        var gml = result.GetFile("Entity.gml")!;

        GmlAssert.ContainsPattern(gml, "function Entity_TakeDamage(self, amount)");
        GmlAssert.ContainsPattern(gml, "Entity_set_CurrentHp(self, Entity_get_CurrentHp(self) - amount);");
        GmlAssert.ContainsPattern(gml, "function Entity_get_CurrentHp(self)");
        GmlAssert.ContainsPattern(gml, "return self.__backing_CurrentHp;");
        GmlAssert.ContainsPattern(gml, "function Entity_set_CurrentHp(self, value)");
        GmlAssert.ContainsPattern(gml, "self.__backing_CurrentHp = value;");
        GmlAssert.NotContainsPattern(gml, "Entity_set_CurrentHp(,");
    }

    [Fact]
    public void Test_ObjectEventFile_PassesSelf()
    {
        var result = Compile("""
            using TypedGML.GameObjects;
            @Object("obj_Entity")
            public class Entity : GameObject {
                public bool IsAlive { get; set; }

                public override void OnStep() {
                    if (not IsAlive) { return; }
                    UpdateState();
                }

                public void UpdateState() { }
            }
            """);

        result.HasErrors.Should().BeFalse(ErrorSummary(result));
        var gml = result.GetFile("obj_Entity/Step_0.gml")!;

        GmlAssert.ContainsPattern(gml, "if (!Entity_get_IsAlive(self))");
        GmlAssert.ContainsPattern(gml, "Entity_UpdateState(self);");
        GmlAssert.NotContainsPattern(gml, "Entity_get_IsAlive()");
        GmlAssert.NotContainsPattern(gml, "Entity_UpdateState();");
    }

    [Fact]
    public void Test_ObjectClassMethod_NativePropertiesUseInstanceVariables()
    {
        var result = Compile("""
            using TypedGML.GameObjects;

            @Object("obj_Entity")
            public class Entity : GameObject {
                public void MoveTo(number targetX, number targetY) {
                    Direction = X + Y;
                    Speed = Direction;
                }
            }
            """);

        result.HasErrors.Should().BeFalse(ErrorSummary(result));
        var gml = result.GetFile("Entity.gml")!;

        GmlAssert.ContainsPattern(gml, "direction = x + y;");
        GmlAssert.ContainsPattern(gml, "speed = direction;");
        GmlAssert.NotContainsPattern(gml, "TypedGML_GameObjects_GameObject_get_X");
        GmlAssert.NotContainsPattern(gml, "TypedGML_GameObjects_GameObject_get_Y");
        GmlAssert.NotContainsPattern(gml, "TypedGML_GameObjects_GameObject_set_Direction");
        GmlAssert.NotContainsPattern(gml, "TypedGML_GameObjects_GameObject_set_Speed");
        GmlAssert.NotContainsPattern(gml, "self.x");
        GmlAssert.NotContainsPattern(gml, "self.direction");
    }

    [Fact]
    public void Test_ObjectConstructor_NativePropertiesUseInstanceVariables()
    {
        var result = Compile("""
            using TypedGML.GameObjects;

            @Object("obj_Entity")
            public class Entity : GameObject {
                public constructor(number x, number y, string layer, number targetX) {
                    X = targetX;
                }
            }
            """);

        result.HasErrors.Should().BeFalse(ErrorSummary(result));
        var gml = result.GetFile("Entity.gml")!;

        GmlAssert.ContainsPattern(gml, "x = targetX;");
        GmlAssert.NotContainsPattern(gml, "TypedGML_GameObjects_GameObject_set_X");
        GmlAssert.NotContainsPattern(gml, "self.x = targetX;");
    }

    private static CompileResult Compile(string source) => CompilerFixture.Compile(source);

    private static string ErrorSummary(CompileResult result) =>
        string.Join(Environment.NewLine, result.Errors.Select(error => $"{error.Code}: {error.Message}"));
}
