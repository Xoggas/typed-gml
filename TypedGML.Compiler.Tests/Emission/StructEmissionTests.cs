using FluentAssertions;
using TypedGML.Compiler.Tests.Helpers;

namespace TypedGML.Compiler.Tests.Emission;

public sealed class StructEmissionTests
{
    [Fact]
    public void Test_StructCreateFunction()
    {
        var gml = Compile("""
            public struct Point {
                public number X;
                public number Y;
                public constructor(number x, number y) { X = x; Y = y; }
            }
            """).GetFile("Point.gml")!;
        GmlAssert.HasFunction(gml, "Point_create");
        GmlAssert.ContainsPattern(gml, "var inst = {};");
        GmlAssert.ContainsPattern(gml, "inst.X = x;");
        GmlAssert.ContainsPattern(gml, "inst.Y = y;");
        GmlAssert.ContainsPattern(gml, "return inst;");
    }

    [Fact]
    public void Test_StructMethodEmitted()
    {
        var gml = Compile("""
            public struct Point {
                public string ToString() { return "point"; }
            }
            """).GetFile("Point.gml")!;
        GmlAssert.HasFunction(gml, "Point_ToString");
        GmlAssert.ContainsPattern(gml, "function Point_ToString(inst)");
    }

    [Fact]
    public void Test_StructDefaultValue()
    {
        var gml = Compile("""
            public struct Point {
                public number X;
                public number Y;
            }
            public class Box<T> {
                public T MakeGeneric() { return default(T); }
                public Point MakePoint() { return default(Point); }
            }
            """).GetFile("Box1.gml")!;
        GmlAssert.ContainsPattern(gml, "__tgml_default(__genericArgs.T)");
        GmlAssert.ContainsPattern(gml, "Point_create()");
    }

    [Fact]
    public void Test_StructStringField_DefaultsToEmptyString()
    {
        var gml = Compile("""
            public struct Label {
                public string Text;
            }
            """).GetFile("Label.gml")!;

        GmlAssert.ContainsPattern(gml, "inst.Text = \"\";");
    }

    private static CompileResult Compile(string source) => CompilerFixture.Compile(source);
}
