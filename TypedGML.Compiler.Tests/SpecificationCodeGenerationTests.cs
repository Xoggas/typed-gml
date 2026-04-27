using TypedGML.Compiler.Tests.Infrastructure;

namespace TypedGML.Compiler.Tests;

public sealed class SpecificationCodeGenerationTests
{
    [Fact]
    public void FileScopedNamespaceEmitsNamespacedScriptPath()
    {
        var result = CompilerAssert.Succeeds(
            """
            namespace Game.UI;

            public class Hud {
                public number Value = 1;
            }
            """);

        var script = result.GetRequiredFile("scripts/Game_UI_Hud.gml");
        Assert.Contains("function Game_UI_Hud_create()", script, StringComparison.Ordinal);
    }

    [Fact]
    public void ConstAndEnumEmitMacros()
    {
        var result = CompilerAssert.Succeeds(
            """
            public class Limits {
                public const number MaxScore = 100;
            }

            public enum Direction {
                North = 0,
                South = 1
            }
            """);

        CompilerAssert.OutputContains(result,
            "#macro Limits_MaxScore 100",
            "#macro Direction_North 0",
            "#macro Direction_South 1");
    }

    [Fact]
    public void AssetAndNativePropertyDecoratorsEmitDirectTargets()
    {
        var result = CompilerAssert.Succeeds(
            """
            using TypedGML.Assets;

            public class SpriteView {
                @NativeProperty("x")
                public number X { get; set; }

                @Asset("SPR_Player")
                public SpriteAsset Sprite { get; }
            }
            """);

        CompilerAssert.OutputContains(result,
            "return self.x;",
            "self.x = value;",
            "return self.SPR_Player;");
    }

    [Fact]
    public void NullOperatorsAndDefaultEmitExpectedFragments()
    {
        var result = CompilerAssert.Succeeds(
            """
            public class Child {
                public number Value = 1;
            }

            public class Holder {
                public Child? Current = null;

                public number Read() {
                    return Current?.Value ?? default(number);
                }

                public string? Missing() {
                    return default(string);
                }
            }
            """);

        CompilerAssert.OutputContains(result,
            "!= undefined ?",
            ": 0)",
            "return undefined;");
    }

    [Fact]
    public void ObjectDecoratorEmitsEventFileAndCreateWrapper()
    {
        var result = CompilerAssert.Succeeds(
            """
            using TypedGML.GameObjects;

            @Object("OBJ_Player")
            public class Player : GameObject {
                public Player(number x, number y, string layer) { }

                @NativeEvent("Create")
                public override void OnCreate() {
                    Health = 10;
                }
            }
            """);

        var eventFile = result.GetRequiredFile("objects/OBJ_Player/Create_0.gml");
        var scriptFile = result.GetRequiredFile("scripts/Player.gml");
        Assert.Contains("health = 10;", eventFile, StringComparison.Ordinal);
        Assert.Contains("instance_create_layer(x, y, layer, OBJ_Player)", scriptFile, StringComparison.Ordinal);
    }

    [Fact]
    public void ArraysAndTryCatchFinallyEmitExpectedSyntax()
    {
        var result = CompilerAssert.Succeeds(
            """
            using TypedGML.Core;

            public class Sample {
                public number[] Values() {
                    return [1, 2, 3];
                }

                public void Run() {
                    try {
                        throw new Exception("boom");
                    } catch (Exception e) {
                        var message = e.message;
                    } finally {
                        var done = true;
                    }
                }
            }
            """);

        CompilerAssert.OutputContains(result,
            "return [1, 2, 3];",
            "try {",
            "catch (",
            "finally {");
    }
}
