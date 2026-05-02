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
}
