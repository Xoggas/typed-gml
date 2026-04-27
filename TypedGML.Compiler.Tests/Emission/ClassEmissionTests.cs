using FluentAssertions;
using TypedGML.Compiler.Tests.Helpers;

namespace TypedGML.Compiler.Tests.Emission;

public sealed class ClassEmissionTests
{
    [Fact]
    public void Test_AbstractClassNoCreateFunction() =>
        GmlAssert.NotContainsPattern(Compile("public abstract class AbstractClass { public abstract void Run(); }").GetFile("AbstractClass.gml")!, "function AbstractClass_create");

    [Fact]
    public void Test_ConcreteClassHasCreateFunction() =>
        GmlAssert.HasFunction(Compile("public class ConcreteClass { }").GetFile("ConcreteClass.gml")!, "ConcreteClass_create");

    [Fact]
    public void Test_OverrideInlined()
    {
        var gml = Compile("""
            public class Parent {
                public virtual number Value() { return 1; }
            }
            public class Child : Parent {
                public override number Value() { return 2; }
            }
            """).GetFile("Child.gml")!;
        var childMethod = FunctionBlock(gml, "function Child_Value");
        GmlAssert.ContainsPattern(childMethod, "return 2;");
        GmlAssert.NotContainsPattern(childMethod, "Parent_Value(");
    }

    [Fact]
    public void Test_BaseCallInlined()
    {
        var gml = Compile("""
            public class Parent {
                public virtual number Value() { var parent_local = 1; return parent_local; }
            }
            public class Child : Parent {
                public override number Value() { return base.Value(); }
            }
            """).GetFile("Child.gml")!;
        var childMethod = FunctionBlock(gml, "function Child_Value");
        GmlAssert.ContainsPattern(childMethod, "parent_local");
        GmlAssert.NotContainsPattern(childMethod, "Parent_Value(");
        GmlAssert.NotContainsPattern(childMethod, "return Value();");
    }

    private static CompileResult Compile(string source) => CompilerFixture.Compile(source);

    private static string FunctionBlock(string gml, string prefix)
    {
        var start = gml.IndexOf(prefix, StringComparison.Ordinal);
        start.Should().BeGreaterThan(-1);
        var next = gml.IndexOf("\n\nfunction ", start + prefix.Length, StringComparison.Ordinal);
        return next < 0 ? gml[start..] : gml[start..next];
    }
}
