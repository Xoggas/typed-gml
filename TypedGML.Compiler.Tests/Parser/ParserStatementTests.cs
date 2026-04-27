using FluentAssertions;
using TypedGML.Compiler.Tests.Helpers;

namespace TypedGML.Compiler.Tests.Parser;

public sealed class ParserStatementTests
{
    [Fact]
    public void DeclarationStatementsParseSuccessfully() =>
        AssertParses("public class DeclarationStatements { public void Run() { var inferred = 1; number typed = inferred; } }");

    [Fact]
    public void BranchingStatementsParseSuccessfully() =>
        AssertParses("public class BranchingStatements { public void Run() { if (true) { var one = 1; } else if (false) { var two = 2; } else { var three = 3; } } }");

    [Fact]
    public void LoopStatementsParseSuccessfully() =>
        AssertParses("public class LoopStatements { public void Run() { while (false) { break; } for (var i = 0; true; i = i) { continue; } for (; true; ) { break; } repeat (2) { var x = 1; } } }");

    [Fact]
    public void SwitchStatementParsesSuccessfully() =>
        AssertParses("public class SwitchStatements { public void Run(number value) { switch (value) { case 1: break; case 2: break; default: break; } } }");

    [Fact]
    public void WithStatementParsesSuccessfully() =>
        AssertParses("""
            using TypedGML.GameObjects;
            @Object("OBJ_WithStatementTarget")
            public class WithStatementTarget : GameObject {
                public void Run() { with (this) { } }
            }
            """);

    [Fact]
    public void ReturnStatementsParseSuccessfully() =>
        AssertParses("public class ReturnStatements { public void Run() { return; } public number Value() { return 1; } }");

    [Fact]
    public void TryCatchFinallyAndThrowParseSuccessfully() =>
        AssertParses("""
            using TypedGML.Core;
            public class TryStatements {
                public void Run() {
                    try { throw new Exception("x"); }
                    catch (Exception e) { var message = e.message; }
                    finally { var done = true; }
                }
            }
            """);

    private static void AssertParses(string source)
    {
        var result = CompilerFixture.Compile(source);
        result.HasErrors.Should().BeFalse(string.Join(Environment.NewLine, result.Errors.Select(error => error.Message)));
    }
}
