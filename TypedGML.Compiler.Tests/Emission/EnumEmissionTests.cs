using FluentAssertions;
using TypedGML.Compiler.Tests.Helpers;

namespace TypedGML.Compiler.Tests.Emission;

public sealed class EnumEmissionTests
{
    [Fact]
    public void Test_EnumMemberAccessEmitsMacroName()
    {
        var result = CompilerFixture.Compile("""
            public enum EntityState { Idle = 0, Moving = 1, Dead = 2 }
            public class Actor {
                public EntityState State;
                public void Run() {
                    var s = EntityState.Idle;
                    State = EntityState.Moving;
                    switch (State) {
                        case EntityState.Dead: break;
                        default: break;
                    }
                }
            }
            """);

        result.HasErrors.Should().BeFalse();
        var gml = result.GetFile("Actor.gml")!;
        GmlAssert.ContainsPattern(gml, "var s = EntityState_Idle;");
        GmlAssert.ContainsPattern(gml, "State = EntityState_Moving;");
        GmlAssert.ContainsPattern(gml, "case EntityState_Dead:");
    }

    [Fact]
    public void Test_StringPlusEnumMember_ConvertsEnumWithGmlString()
    {
        var result = CompilerFixture.Compile("""
            public enum BossPhase { Phase1 = 0, Phase2 = 1 }
            public class Boss {
                public void Run() {
                    string message = "Phase: " + BossPhase.Phase1;
                }
            }
            """);

        result.HasErrors.Should().BeFalse();
        var gml = result.GetFile("Boss.gml")!;
        GmlAssert.ContainsPattern(gml, "\"Phase: \" + string(BossPhase_Phase1)");
    }

    [Fact]
    public void Test_StringPlusEnumVariable_ConvertsEnumWithGmlString()
    {
        var result = CompilerFixture.Compile("""
            public enum BossPhase { Phase1 = 0, Phase2 = 1 }
            public class Boss {
                public void Run(BossPhase phase) {
                    string message = "State: " + phase;
                }
            }
            """);

        result.HasErrors.Should().BeFalse();
        var gml = result.GetFile("Boss.gml")!;
        GmlAssert.ContainsPattern(gml, "\"State: \" + string(phase)");
    }
}
