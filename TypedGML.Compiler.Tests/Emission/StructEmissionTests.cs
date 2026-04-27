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
                public Point(number x, number y) { X = x; Y = y; }
            }
            """).GetFile("Point.gml")!;
        GmlAssert.HasFunction(gml, "Point_create");
        GmlAssert.ContainsPattern(gml, "var self = {};");
        GmlAssert.ContainsPattern(gml, "self.X = x;");
        GmlAssert.ContainsPattern(gml, "self.Y = y;");
        GmlAssert.ContainsPattern(gml, "return self;");
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
        GmlAssert.ContainsPattern(gml, "function Point_ToString(self)");
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
            """).GetFile("Box.gml")!;
        GmlAssert.ContainsPattern(gml, "__tgml_default(__genericArgs.T)");
        GmlAssert.ContainsPattern(gml, "Point_create()");
    }

    private static CompileResult Compile(string source) => CompilerFixture.Compile(source);
}
