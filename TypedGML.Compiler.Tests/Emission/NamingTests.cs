using FluentAssertions;
using TypedGML.Compiler.Tests.Helpers;

namespace TypedGML.Compiler.Tests.Emission;

public sealed class NamingTests
{
    [Fact] public void Test_ClassNameNoNamespace() => Compile("public class Foo { }").GetFile("Foo.gml").Should().NotBeNull();
    [Fact] public void Test_ClassNameWithNamespace() => Compile("namespace A.B; public class Foo { }").GetFile("A_B_Foo.gml").Should().NotBeNull();
    [Fact] public void Test_MethodName() => GmlAssert.HasFunction(Compile("public class Foo { public void Bar() { } }").GetFile("Foo.gml")!, "Foo_Bar");
    [Fact] public void Test_PropertyGetterName() { var gml = Compile("public class Foo { public number X { get; set; } }").GetFile("Foo.gml")!; GmlAssert.HasFunction(gml, "Foo_get_X"); GmlAssert.NotContainsPattern(gml, "Foo_X_get"); }
    [Fact] public void Test_PropertySetterName() => GmlAssert.HasFunction(Compile("public class Foo { public number X { get; set; } }").GetFile("Foo.gml")!, "Foo_set_X");
    [Fact] public void Test_ConstructorName() => GmlAssert.HasFunction(Compile("public struct Foo { public number X; }").GetFile("Foo.gml")!, "Foo_create");
    [Fact] public void Test_OperatorName() => GmlAssert.HasFunction(Compile("public class Foo { public static Foo operator +(Foo a, Foo b) { return a; } }").GetFile("Foo.gml")!, "Foo_op_add");

    [Fact]
    public void Test_ConstMacro()
    {
        var gml = Compile("public class Foo { public const number Max = 100; }").GetFile("Foo.gml")!;
        GmlAssert.HasMacro(gml, "Foo_Max", "100");
    }

    [Fact]
    public void Test_EnumMacro()
    {
        var gml = Compile("public enum Dir { North = 0, South = 1 }").GetFile("Dir.gml")!;
        GmlAssert.HasMacro(gml, "Dir_North", "0");
        GmlAssert.HasMacro(gml, "Dir_South", "1");
    }

    private static CompileResult Compile(string source) => CompilerFixture.Compile(source);
}
