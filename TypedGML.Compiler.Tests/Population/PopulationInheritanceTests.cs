using FluentAssertions;
using TypedGML.Compiler.Diagnostics;
using TypedGML.Compiler.Tests.Helpers;

namespace TypedGML.Compiler.Tests.Population;

public sealed class PopulationInheritanceTests
{
    [Fact]
    public void Test_BaseClassResolved() =>
        Compile("public class A { } public class B : A { }").HasErrors.Should().BeFalse();

    [Fact]
    public void Test_InterfaceResolved() =>
        Compile("public interface IFoo { } public class Bar : IFoo { public void IFoo_method() { } }")
            .HasErrors.Should().BeFalse();

    [Fact]
    public void Test_CircularInheritance_TGML0002() =>
        Compile("public class A : B { } public class B : A { }")
            .HasError(DiagnosticCode.CircularInheritance)
            .Should().BeTrue();

    [Fact]
    public void Test_UnknownBaseClass_Error()
    {
        var result = Compile("public class Foo : NonExistent { }");
        result.HasErrors.Should().BeTrue();
        result.Errors.Should().Contain(error =>
            error.Message.Contains("NonExistent", StringComparison.Ordinal) &&
            error.Message.Contains("could not be resolved", StringComparison.Ordinal));
    }

    [Fact]
    public void Test_NamespaceConflict_TGML0031() =>
        Compile("namespace A { public class B { } } namespace A.B { public class C { } }")
            .HasError(DiagnosticCode.NamespaceTypeNameConflict)
            .Should().BeTrue();

    private static CompileResult Compile(string source) =>
        PopulationFixture.Compile(source);
}
