using FluentAssertions;
using TypedGML.Compiler.Diagnostics;
using TypedGML.Compiler.Tests.Helpers;

namespace TypedGML.Compiler.Tests.Population;

public sealed class PopulationTypeTests
{
    [Fact]
    public void Test_ClassRegisteredInSymbolTable() =>
        Compile("public class Foo { }").HasErrors.Should().BeFalse();

    [Fact]
    public void Test_StructRegisteredInSymbolTable() =>
        Compile("public struct Bar { }").HasErrors.Should().BeFalse();

    [Fact]
    public void Test_InterfaceRegisteredInSymbolTable() =>
        Compile("public interface IFoo { }").HasErrors.Should().BeFalse();

    [Fact]
    public void Test_EnumRegisteredInSymbolTable() =>
        Compile("public enum Color { Red = 0 }").HasErrors.Should().BeFalse();

    [Fact]
    public void Test_DelegateRegisteredInSymbolTable() =>
        Compile("public delegate void MyDel();").HasErrors.Should().BeFalse();

    [Fact]
    public void Test_DuplicateTypeName_TGML0001() =>
        Compile("public class Foo { } public class Foo { }")
            .HasError(DiagnosticCode.DuplicateTypeName)
            .Should().BeTrue();

    [Fact]
    public void Test_SameNameDifferentNamespace_NoError() =>
        Compile("namespace A { public class Foo { } } namespace B { public class Foo { } }")
            .HasErrors.Should().BeFalse();

    private static CompileResult Compile(string source) =>
        PopulationFixture.Compile(source);
}
