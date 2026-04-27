using TypedGML.Compiler.Tests.Infrastructure;

namespace TypedGML.Compiler.Tests;

public sealed class EmissionTests
{
    [Fact]
    public void StaticMembersEmitInsideStaticCtorWithPragma()
    {
        var result = CompilerTestDriver.Compile(
            """
            public class Settings {
                public static number Volume = 1;

                static Settings() {
                    Volume = 2;
                }
            }
            """);

        Assert.True(result.Succeeded, result.StandardError);
        Assert.Contains("function Settings_static_ctor()", result.AllOutput);
        Assert.Contains("global.Settings_Volume = 1;", result.AllOutput);
        Assert.Contains("gml_pragma(\"global\", \"Settings_static_ctor()\");", result.AllOutput);
    }

    [Fact]
    public void ObjectConstructionUsesInstanceCreateLayer()
    {
        var result = CompilerTestDriver.Compile(
            """
            using TypedGML.GameObjects;

            @Object("OBJ_Player")
            public class Player : GameObject {
                public Player(number x, number y, string layer) { }
            }

            public class Factory {
                public Player Make() {
                    return new Player(10, 20, "Instances");
                }
            }
            """);

        Assert.True(result.Succeeded, result.StandardError);
        Assert.Contains("instance_create_layer(10, 20, \"Instances\", OBJ_Player)", result.AllOutput);
    }

    [Fact]
    public void NullConditionalUsesUndefinedGuard()
    {
        var result = CompilerTestDriver.Compile(
            """
            public class Child {
                public number Value = 1;
            }

            public class Holder {
                public Child? Current = null;

                public number? Read() {
                    return Current?.Value;
                }
            }
            """);

        Assert.True(result.Succeeded, result.StandardError);
        Assert.Contains("!= undefined ?", result.AllOutput);
    }

    [Fact]
    public void TypeofAndNameofEmitStringLiterals()
    {
        var result = CompilerTestDriver.Compile(
            """
            public class Sample {
                public string TypeName() { return typeof(Sample); }
                public string MemberName() { return nameof(TypeName); }
            }
            """);

        Assert.True(result.Succeeded, result.StandardError);
        Assert.Contains("return \"Sample\";", result.AllOutput);
        Assert.Contains("return \"TypeName\";", result.AllOutput);
    }

    [Fact]
    public void EnumMembersEmitMacros()
    {
        var result = CompilerTestDriver.Compile(
            """
            public enum Direction {
                North = 0,
                South = 1
            }
            """);

        Assert.True(result.Succeeded, result.StandardError);
        Assert.Contains("#macro Direction_North 0", result.AllOutput);
        Assert.Contains("#macro Direction_South 1", result.AllOutput);
    }
}
