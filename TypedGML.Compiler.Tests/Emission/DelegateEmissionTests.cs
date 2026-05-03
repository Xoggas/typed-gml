using FluentAssertions;
using TypedGML.Compiler.Tests.Helpers;

namespace TypedGML.Compiler.Tests.Emission;

public sealed class DelegateEmissionTests
{
    [Fact]
    public void Test_DelegateSubscribe() =>
        GmlAssert.ContainsPattern(CompileInMethod("Callback myDel; myDel += Handler;").GetFile("DelegateHost.gml")!, "myDel[array_length(myDel)] = method(self, DelegateHost_Handler);");

    [Fact]
    public void Test_DelegateUnsubscribe() =>
        GmlAssert.ContainsPattern(CompileInMethod("Callback myDel; myDel -= Handler;").GetFile("DelegateHost.gml")!, "myDel = __tgml_delegate_remove(myDel, method(self, DelegateHost_Handler));");

    [Fact]
    public void Test_DelegateInvoke() =>
        GmlAssert.ContainsPattern(CompileInMethod("Callback myDel; myDel(arg1, arg2);", "number arg1, number arg2").GetFile("DelegateHost.gml")!, "__tgml_invoke_delegate(myDel, arg1, arg2);");

    [Fact]
    public void Test_EventInvoke()
    {
        var result = CompilerFixture.Compile("""
            public delegate void Action();
            public delegate void NumberAction(number value);
            public delegate void StateAction(EntityState state);
            public enum EntityState { Idle = 0, Moving = 1 }
            public class DelegateHost {
                public event Action OnDeath;
                public event NumberAction OnDamageTaken;
                public event StateAction OnStateChanged;
                public void Run() {
                    OnDeath();
                    OnDamageTaken(10);
                    OnStateChanged(EntityState.Moving);
                }
            }
            """);

        result.HasErrors.Should().BeFalse(string.Join(Environment.NewLine, result.Errors.Select(error => error.Message)));
        var gml = result.GetFile("DelegateHost.gml")!;
        GmlAssert.ContainsPattern(gml, "__tgml_invoke_delegate(self.__event_OnDeath);");
        GmlAssert.ContainsPattern(gml, "__tgml_invoke_delegate(self.__event_OnDamageTaken, 10);");
        GmlAssert.ContainsPattern(gml, "__tgml_invoke_delegate(self.__event_OnStateChanged, EntityState_Moving);");
    }

    [Fact]
    public void Test_ActionLambdaInitializer()
    {
        var result = CompilerFixture.Compile("public class LambdaHost { public void Run() { Action<number> fn = (number x) => x * 2; } }");

        result.HasErrors.Should().BeFalse(string.Join(Environment.NewLine, result.Errors.Select(error => error.Message)));
        var gml = result.GetFile("LambdaHost.gml")!;
        GmlAssert.ContainsPattern(gml, "var fn = function(x) { x * 2; };");
        GmlAssert.NotContainsPattern(gml, "return x * 2;");
    }

    [Fact]
    public void Test_FuncBlockLambdaInitializer()
    {
        var result = CompilerFixture.Compile("""
            public class LambdaHost {
                public void Run() {
                    Func<number, number> fn = (number x) => {
                        number y = x + 1;
                        return y;
                    };
                }
            }
            """);

        result.HasErrors.Should().BeFalse(string.Join(Environment.NewLine, result.Errors.Select(error => error.Message)));
        var gml = result.GetFile("LambdaHost.gml")!;
        GmlAssert.ContainsPattern(gml, "var fn = function(x)");
        GmlAssert.ContainsPattern(gml, "var y = x + 1;");
        GmlAssert.ContainsPattern(gml, "return y;");
    }

    [Fact]
    public void Test_DelegateLocalLifecycle()
    {
        var result = CompilerFixture.Compile("""
            public delegate void Handler(number value);
            public class T {
                public void SomeFunc(number value) { }
                public void Run() {
                    Handler h;
                    h += SomeFunc;
                    h -= SomeFunc;
                    h(42);
                }
            }
            """);

        result.HasErrors.Should().BeFalse();
        var gml = result.GetFile("/T.gml")!;
        GmlAssert.ContainsPattern(gml, "var h = [];");
        GmlAssert.ContainsPattern(gml, "h[array_length(h)] = method(self, T_SomeFunc);");
        GmlAssert.ContainsPattern(gml, "h = __tgml_delegate_remove(h, method(self, T_SomeFunc));");
        GmlAssert.ContainsPattern(gml, "__tgml_invoke_delegate(h, 42);");
    }

    [Fact]
    public void Test_EventSubscribe_UsesArrayAppendAndBoundMethod()
    {
        var result = CompilerFixture.Compile("""
            public delegate void Action();
            public class Player {
                public event Action OnDeath;
            }
            public class GameController {
                private Player _player;
                public void HandlePlayerDeath() { }
                public void HandleEnemyDeath() { }
                public void Run(Player enemy) {
                    _player.OnDeath += HandlePlayerDeath;
                    enemy.OnDeath += HandleEnemyDeath;
                }
            }
            """);

        result.HasErrors.Should().BeFalse(string.Join(Environment.NewLine, result.Errors.Select(error => error.Message)));
        var gml = result.GetFile("GameController.gml")!;
        GmlAssert.ContainsPattern(gml, "self._player.__event_OnDeath[array_length(self._player.__event_OnDeath)] = method(self, GameController_HandlePlayerDeath);");
        GmlAssert.ContainsPattern(gml, "enemy.__event_OnDeath[array_length(enemy.__event_OnDeath)] = method(self, GameController_HandleEnemyDeath);");
    }

    private static CompileResult CompileInMethod(string statement, string parameters = "") =>
        CompilerFixture.Compile($$"""
            public delegate void Callback(number value1, number value2);
            public class DelegateHost {
                public void Handler(number value1, number value2) { }
                public void Run({{parameters}}) {
                    {{statement}}
                }
            }
            """);
}
