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

    private static CompileResult Compile(string source) => CompilerFixture.Compile(source);

    private static string ErrorSummary(CompileResult result) =>
        string.Join(Environment.NewLine, result.Errors.Select(error => $"{error.Code}: {error.Message}"));
}
