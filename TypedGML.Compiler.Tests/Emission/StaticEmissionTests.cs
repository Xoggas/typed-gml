using FluentAssertions;
using TypedGML.Compiler.Tests.Helpers;

namespace TypedGML.Compiler.Tests.Emission;

public sealed class StaticEmissionTests
{
    [Fact]
    public void Test_StaticMethod_EmittedInStaticCtor()
    {
        var gml = Compile("public class MathHost { public static number Abs(number x) { return x; } }").GetFile("MathHost.gml")!;
        GmlAssert.ContainsPattern(gml, "global.MathHost_Abs = function(x) {");
        GmlAssert.HasPragma(gml, "MathHost_static_ctor");
    }

    [Fact]
    public void Test_StaticField_EmittedInStaticCtor() =>
        GmlAssert.ContainsPattern(Compile("public class Config { public static number X = 5; }").GetFile("Config.gml")!, "global.Config_X = 5;");

    [Fact]
    public void Test_StaticStringField_DefaultsToEmptyString() =>
        GmlAssert.ContainsPattern(Compile("public class Config { public static string Name; }").GetFile("Config.gml")!, "global.Config_Name = \"\";");

    [Fact]
    public void Test_StaticProperty_EmittedInStaticCtor()
    {
        var gml = Compile("public class Config { public static number Volume { get; set; } }").GetFile("Config.gml")!;
        GmlAssert.ContainsPattern(gml, "global.Config__Volume = 0;");
        GmlAssert.ContainsPattern(gml, "global.Config_get_Volume = function() {");
        GmlAssert.ContainsPattern(gml, "global.Config_set_Volume = function(value) {");
    }

    [Fact]
    public void Test_StaticStringProperty_DefaultsToEmptyString() =>
        GmlAssert.ContainsPattern(Compile("public class Config { public static string Name { get; set; } }").GetFile("Config.gml")!, "global.Config__Name = \"\";");

    [Fact]
    public void Test_StaticCallSite() =>
        GmlAssert.ContainsPattern(Compile("public class MathHost { public static number Abs(number x) { return x; } public number Run() { return MathHost.Abs(1); } }").GetFile("MathHost.gml")!, "return global.MathHost_Abs(1);");

    [Fact]
    public void Test_ConstNotGlobal()
    {
        var gml = Compile("public class Config { public const number C = 1; }").GetFile("Config.gml")!;
        GmlAssert.HasMacro(gml, "Config_C", "1");
        GmlAssert.NotContainsPattern(gml, "global.Config_C");
        GmlAssert.NotContainsPattern(gml, "Config_static_ctor");
    }

    [Fact]
    public void Test_StaticCtorExplicitAssignment_SuppressesFieldInitializer()
    {
        var gml = Compile("public class Config { public static number X = 5; static constructor() { X = 5; } }").GetFile("Config.gml")!;
        Count(gml, "global.Config_X = 5;").Should().Be(1);
        GmlAssert.HasPragma(gml, "Config_static_ctor");
    }

    [Fact]
    public void Test_StaticCtorExplicitPlayerAssignments_EmitOnce()
    {
        var gml = Compile("""
            public class Player {
                public static number TotalPlayTime = 0;
                public static number DeathCount = 0;
                static constructor() {
                    TotalPlayTime = 0;
                    DeathCount = 0;
                }
            }
            """).GetFile("Player.gml")!;
        Count(gml, "global.Player_TotalPlayTime = 0;").Should().Be(1);
        Count(gml, "global.Player_DeathCount = 0;").Should().Be(1);
        GmlAssert.HasPragma(gml, "Player_static_ctor");
    }

    [Fact]
    public void Test_StaticConstructorKeyword_EmitsStaticCtorFunction()
    {
        var gml = Compile("public class Foo { static constructor() { } }").GetFile("Foo.gml")!;
        GmlAssert.ContainsPattern(gml, "function Foo_static_ctor()");
    }

    private static CompileResult Compile(string source) => CompilerFixture.Compile(source);

    private static int Count(string value, string pattern) =>
        value.Split(pattern, StringSplitOptions.None).Length - 1;
}
