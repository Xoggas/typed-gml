using FluentAssertions;
using TypedGML.Compiler.Diagnostics;
using TypedGML.Compiler.Tests.Helpers;

namespace TypedGML.Compiler.Tests.Verification;

public sealed class ClassMemberTests
{
    [Fact] public void Test_AbstractMemberNotImplemented_TGML0003() => AssertHasError("public abstract class Base { public abstract void Foo(); } public class Child : Base { }", DiagnosticCode.AbstractMemberNotImplemented);
    [Fact] public void Test_AbstractMemberImplemented_Valid() => AssertValid("public abstract class Base { public abstract void Foo(); } public class Child : Base { public override void Foo() { } }");
    [Fact] public void Test_InstantiateAbstractClass_TGML0004() => AssertHasError("public abstract class AbstractClass { } public class Host { public void Run() { var value = new AbstractClass(); } }", DiagnosticCode.AbstractClassInstantiation);
    [Fact] public void Test_SealedClassSubclassed_TGML0005() => AssertHasError("public sealed class A { } public class B : A { }", DiagnosticCode.SealedClassInheritance);
    [Fact] public void Test_InterfaceMemberNotImplemented_TGML0006() => AssertHasError("public interface IFoo { void Foo(); } public class FooHost : IFoo { }", DiagnosticCode.InterfaceMemberNotImplemented);
    [Fact] public void Test_InterfaceMemberImplemented_Valid() => AssertValid("public interface IFoo { void Foo(); } public class FooHost : IFoo { public void Foo() { } }");
    [Fact] public void Test_OverrideWithNoVirtualAncestor_TGML0007() => AssertHasError("public class Base { public void Foo() { } } public class Child : Base { public override void Foo() { } }", DiagnosticCode.MissingOverrideTarget);
    [Fact] public void Test_OverrideValid() => AssertValid("public class Base { public virtual void Foo() { } } public class Child : Base { public override void Foo() { } }");
    [Fact] public void Test_PrivateMemberAccessedExternally_TGML0008() => AssertHasError("public class Owner { private number Value; } public class Other { public void Run() { var owner = new Owner(); var value = owner.Value; } }", DiagnosticCode.AccessViolation);
    [Fact] public void Test_ProtectedMemberAccessedFromSubclass_Valid() => AssertValid("public class Base { protected number Value; } public class Child : Base { public number Read() { return Value; } }");
    [Fact] public void Test_ProtectedMemberAccessedExternally_TGML0008() => AssertHasError("public class Base { protected number Value; } public class Other { public void Run() { var owner = new Base(); var value = owner.Value; } }", DiagnosticCode.AccessViolation);
    [Fact] public void Test_ReadonlyFieldAssignedOutsideCtor_TGML0009() => AssertHasError("public class ReadonlyHost { public readonly number X; public void Run() { X = 1; } }", DiagnosticCode.ReadonlyFieldAssignmentOutsideConstructor);
    [Fact] public void Test_ReadonlyFieldAssignedInCtor_Valid() => AssertValid("public class ReadonlyHost { public readonly number X; public ReadonlyHost() { X = 1; } }");
    [Fact] public void Test_ConstFieldNonConstantExpr_TGML0010() => AssertHasError("public class ConstHost { public static number SomeMethod() { return 1; } public const number X = SomeMethod(); }", DiagnosticCode.NonConstantConstField);
    [Fact] public void Test_ConstFieldLiteral_Valid() => AssertValid("public class ConstHost { public const number X = 42; }");
    [Fact] public void Test_StructInheritsClass_TGML0032() => AssertHasError("public class SomeClass { } public struct S : SomeClass { }", DiagnosticCode.InvalidStructInheritance);
    [Fact] public void Test_StructImplementsInterface_Valid() => AssertValid("public interface IRunner { void Run(); } public struct Runner : IRunner { public void Run() { } }");
    [Fact] public void Test_DuplicateMethodSameSignature_Error() => AssertHasError("public class OverloadHost { public void Run() { } public void Run() { } }", DiagnosticCode.TypeMismatch);
    [Fact] public void Test_OverloadedMethodDifferentParams_Valid() => AssertValid("public class OverloadHost { public void Run() { } public void Run(number x) { } }");
    [Fact] public void Test_StaticOnConstructor_TGML0026() => AssertHasError("public class StaticCtorHost { public static StaticCtorHost() { } }", DiagnosticCode.InvalidStaticModifierTarget);
    [Fact] public void Test_StaticOnIndexer_TGML0026() => AssertHasError("public class IndexedHost { public static number this[number index] { get { return index; } } }", DiagnosticCode.InvalidStaticModifierTarget);
    [Fact] public void Test_StaticInsideInterface_TGML0027() => AssertHasError("public interface IStaticHost { static void Run(); }", DiagnosticCode.StaticMemberInInterface);

    private static void AssertValid(string source)
    {
        var result = CompilerFixture.Compile(source);
        result.HasErrors.Should().BeFalse(string.Join(Environment.NewLine, result.Errors.Select(error => error.Message)));
    }

    private static void AssertHasError(string source, DiagnosticCode code)
    {
        var result = CompilerFixture.Compile(source);
        result.HasError(code).Should().BeTrue(string.Join(Environment.NewLine, result.Errors.Select(error => $"{error.Code}: {error.Message}")));
    }
}
