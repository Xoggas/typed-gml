using FluentAssertions;
using TypedGML.Compiler.Diagnostics;
using TypedGML.Compiler.Tests.Helpers;

namespace TypedGML.Compiler.Tests.Emission;

public sealed class BaseCallEmissionTests
{
    [Fact]
    public void Test_BaseCall_InlinesParentBody()
    {
        var result = Compile("""
            using TypedGML.Utils;

            public class Base {
                public virtual void DoSomething() { Debug.Log("I'm a base!"); }
            }
            public class Child : Base {
                public override void DoSomething() { base.DoSomething(); }
            }
            """);

        result.HasErrors.Should().BeFalse(ErrorText(result));
        var childMethod = FunctionBlock(result.GetFile("Child.gml")!, "function Child_DoSomething");
        GmlAssert.ContainsPattern(childMethod, "global.TypedGML_Utils_Debug_Log(\"I'm a base!\");");
        GmlAssert.NotContainsPattern(childMethod, "DoSomething();");
        GmlAssert.NotContainsPattern(childMethod, "Base_DoSomething(");
        GmlAssert.NotContainsPattern(childMethod, "(function()");
    }

    [Fact]
    public void Test_ReturnBaseCall_InlinesParentReturnsWithoutIife()
    {
        var result = Compile("""
            public class Account {
                public number Balance;
                public virtual bool Withdraw(number amount) {
                    if (amount <= 0) {
                        return false;
                    }
                    if (amount > Balance) {
                        return false;
                    }
                    Balance = Balance - amount;
                    return true;
                }
            }
            public class FeeAccount : Account {
                public override bool Withdraw(number amount) {
                    return base.Withdraw(amount);
                }
            }
            """);

        result.HasErrors.Should().BeFalse(ErrorText(result));
        var childMethod = FunctionBlock(result.GetFile("FeeAccount.gml")!, "function FeeAccount_Withdraw");
        GmlAssert.ContainsPattern(childMethod, "if ((amount <= 0))");
        GmlAssert.ContainsPattern(childMethod, "return false;");
        GmlAssert.ContainsPattern(childMethod, "return true;");
        GmlAssert.NotContainsPattern(childMethod, "(function()");
        GmlAssert.NotContainsPattern(childMethod, "return Account_Withdraw(");
    }

    [Fact]
    public void Test_BaseCall_VarInitializer_CapturesReturnValue()
    {
        var result = Compile("""
            public class Parent {
                public virtual number Value() {
                    return 7;
                }
            }
            public class Child : Parent {
                public override number Value() {
                    var x = base.Value();
                    return x;
                }
            }
            """);

        result.HasErrors.Should().BeFalse(ErrorText(result));
        var childMethod = FunctionBlock(result.GetFile("Child.gml")!, "function Child_Value");
        GmlAssert.ContainsPattern(childMethod, "var __base_result;");
        GmlAssert.ContainsPattern(childMethod, "__base_result = 7;");
        GmlAssert.ContainsPattern(childMethod, "var x = __base_result;");
        GmlAssert.NotContainsPattern(childMethod, "(function()");
    }

    [Fact]
    public void Test_BaseCall_NonExistentMethod_Error()
    {
        var result = Compile("""
            public class Base {
                public virtual void DoSomething() { }
            }
            public class Child : Base {
                public override void DoSomething() { base.DoSomethingElse(); }
            }
            """);

        result.HasError(DiagnosticCode.MissingOverrideTarget).Should().BeTrue(ErrorText(result));
        result.Errors.Should().Contain(error => error.Message.Contains("DoSomethingElse", StringComparison.Ordinal));
    }

    [Fact]
    public void Test_BaseCall_PassesArgsCorrectly()
    {
        var result = Compile("""
            using TypedGML.Utils;

            public class Base {
                public virtual void LogMessage(string message) { Debug.Log(message); }
            }
            public class Child : Base {
                public override void LogMessage(string text) { base.LogMessage(text); }
            }
            """);

        result.HasErrors.Should().BeFalse(ErrorText(result));
        var childMethod = FunctionBlock(result.GetFile("Child.gml")!, "function Child_LogMessage");
        GmlAssert.ContainsPattern(childMethod, "global.TypedGML_Utils_Debug_Log(text);");
        GmlAssert.NotContainsPattern(childMethod, "message");
    }

    [Fact]
    public void Test_BaseCall_ChainedInheritance()
    {
        var result = Compile("""
            using TypedGML.Utils;

            public class Base {
                public virtual void DoSomething() { Debug.Log("grandparent"); }
            }
            public class Middle : Base {
                public override void DoSomething() { base.DoSomething(); }
            }
            public class Child : Middle {
                public override void DoSomething() { base.DoSomething(); }
            }
            """);

        result.HasErrors.Should().BeFalse(ErrorText(result));
        var childMethod = FunctionBlock(result.GetFile("Child.gml")!, "function Child_DoSomething");
        GmlAssert.ContainsPattern(childMethod, "global.TypedGML_Utils_Debug_Log(\"grandparent\");");
        GmlAssert.NotContainsPattern(childMethod, "Middle_DoSomething(");
        GmlAssert.NotContainsPattern(childMethod, "Base_DoSomething(");
    }

    private static CompileResult Compile(string source) => CompilerFixture.Compile(source);

    private static string FunctionBlock(string gml, string prefix)
    {
        var start = gml.IndexOf(prefix, StringComparison.Ordinal);
        start.Should().BeGreaterThan(-1);
        var next = gml.IndexOf("\n\nfunction ", start + prefix.Length, StringComparison.Ordinal);
        return next < 0 ? gml[start..] : gml[start..next];
    }

    private static string ErrorText(CompileResult result) =>
        string.Join(Environment.NewLine, result.Errors.Select(error => $"{error.Code}: {error.Message}"));
}
