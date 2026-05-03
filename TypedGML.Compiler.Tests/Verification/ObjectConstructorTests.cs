using FluentAssertions;
using TypedGML.Compiler.Diagnostics;
using TypedGML.Compiler.Tests.Helpers;

namespace TypedGML.Compiler.Tests.Verification;

public sealed class ObjectConstructorTests
{
    [Fact]
    public void Test_ObjectCtor_BaseProvidesCoordsAndLayer_Valid()
    {
        var result = CompilerFixture.Compile("""
            using TypedGML.GameObjects;
            @Object("obj_Player")
            public class Player : GameObject {
                public constructor() : base(0, 0, "layer_default") { }
            }
            public class T {
                public void test() {
                    var player = new Player();
                }
            }
            """);

        result.HasErrors.Should().BeFalse(string.Join(Environment.NewLine, result.Errors.Select(error => error.Message)));
        result.AllOutput().Should().Contain("instance_create_layer(0, 0, \"layer_default\", obj_Player)");
    }

    [Fact]
    public void Test_ObjectCtor_NeitherParamsNorBaseProvideCoords_TGML0025()
    {
        var result = CompilerFixture.Compile("""
            using TypedGML.GameObjects;
            @Object("obj_Player")
            public class Player : GameObject {
                public constructor() { }
            }
            public class T {
                public void test() {
                    var player = new Player();
                }
            }
            """);

        result.HasError(DiagnosticCode.InvalidObjectConstructorArgumentCount)
            .Should().BeTrue(string.Join(Environment.NewLine, result.Errors.Select(error => $"{error.Code}: {error.Message}")));
    }
}
