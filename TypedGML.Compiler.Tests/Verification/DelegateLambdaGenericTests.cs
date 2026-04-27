using FluentAssertions;
using TypedGML.Compiler.Diagnostics;
using TypedGML.Compiler.Tests.Helpers;

namespace TypedGML.Compiler.Tests.Verification;

public sealed class DelegateLambdaGenericTests
{
    [Fact] public void Test_DelegateSignatureMismatch_TGML0028() => AssertHasError("public delegate void NumberHandler(number value); public class Host { public NumberHandler Handler; public void HandleText(string value) { } public void Run() { Handler += HandleText; } }", DiagnosticCode.DelegateSignatureMismatch);
    [Fact] public void Test_DelegateSignatureMatch_Valid() => AssertValid("public delegate void NumberHandler(number value); public class Host { public NumberHandler Handler; public void HandleNumber(number value) { } public void Run() { Handler += HandleNumber; } }");
    [Fact] public void Test_EventAssignedDirectlyExternally_TGML0029() => AssertHasError("public delegate void NumberHandler(number value); public class Publisher { public event NumberHandler Changed; } public class Subscriber { public void Handle(number value) { } public void Run() { var publisher = new Publisher(); publisher.Changed = Handle; } }", DiagnosticCode.InvalidExternalEventAssignment);
    [Fact] public void Test_EventSubscribeExternally_Valid() => AssertValid("public delegate void NumberHandler(number value); public class Publisher { public event NumberHandler Changed; } public class Subscriber { public void Handle(number value) { } public void Run() { var publisher = new Publisher(); publisher.Changed += Handle; } }");
    [Fact] public void Test_LambdaClosureCapture_TGML0012() => AssertHasError("public delegate void NumberHandler(number value); public class Host { public NumberHandler Handler; public void Run() { number outer = 1; Handler += (number x) => { var copy = outer; }; } }", DiagnosticCode.LambdaCapturesOuterScope);
    [Fact] public void Test_LambdaNoCapture_Valid() => AssertValid("public delegate void NumberHandler(number value); public class Host { public NumberHandler Handler; public void Run() { Handler += (number x) => { var copy = x; }; } }");
    [Fact] public void Test_LambdaTypeMismatch_Error() => AssertHasError("public delegate void NumberHandler(number value); public class Host { public NumberHandler Handler; public void Run() { Handler += (string x) => { }; } }", DiagnosticCode.DelegateSignatureMismatch);
    [Fact] public void Test_GenericConstraintSatisfied_Valid() => AssertValid("public class MyClass { } public class Foo<T : MyClass> { } public class Host { public void Run() { var value = new Foo<MyClass>(); } }");
    [Fact] public void Test_GenericConstraintNotSatisfied_TGML0016() => AssertHasError("public class MyClass { } public class OtherClass { } public class Foo<T : MyClass> { } public class Host { public void Run() { var value = new Foo<OtherClass>(); } }", DiagnosticCode.GenericConstraintNotSatisfied);
    [Fact] public void Test_DecoratorOnWrongTarget_Error() => AssertHasError("@NativeEvent(\"Create\") public class Host { public number Value; }", DiagnosticCode.TypeMismatch);
    [Fact] public void Test_ObjectDecoratorMissingString_TGML0023() => AssertHasError("using TypedGML.GameObjects; @Object public class MissingNameObject : GameObject { }", DiagnosticCode.MissingObjectDecoratorName);
    [Fact] public void Test_MultipleObjectDecorators_TGML0024() => AssertHasError("using TypedGML.GameObjects; @Object(\"OBJ_First\") @Object(\"OBJ_Second\") public class MultiObject : GameObject { }", DiagnosticCode.MultipleObjectDecorators);
    [Fact] public void Test_ObjectClassWithoutGameObjectBase_TGML0046() => AssertHasError("@Object(\"OBJ_Bad\") public class BadObject { }", DiagnosticCode.ObjectDecoratorWithoutGameObject);
    [Fact] public void Test_GameObjectSubclassWithoutObject_TGML0045() => AssertHasError("using TypedGML.GameObjects; public class BareGameObject : GameObject { }", DiagnosticCode.GameObjectMissingObjectDecorator);
    [Fact] public void Test_StaticCtorDuplicate_TGML0041() => AssertHasError("public class Config { static Config() { } static Config() { } }", DiagnosticCode.DuplicateStaticConstructor);
    [Fact] public void Test_StaticCtorWithParams_TGML0043() => AssertHasError("public class Config { static Config(number value) { } }", DiagnosticCode.InvalidStaticConstructorUsage);
    [Fact] public void Test_StaticCtorCrossClassRef_TGML0042() => AssertHasError("public class OtherConfig { public static number Value = 1; } public class Config { static Config() { var current = OtherConfig.Value; } }", DiagnosticCode.CrossTypeStaticReference);
    [Fact] public void Test_NativeEventUnknownName_TGML0037() => AssertHasError("public class Host { @NativeEvent(\"NotARealEvent\") public void Run() { } }", DiagnosticCode.UnknownNativeEventName);

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
