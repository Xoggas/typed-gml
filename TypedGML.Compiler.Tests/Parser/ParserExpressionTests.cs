using FluentAssertions;
using TypedGML.Compiler.Tests.Helpers;

namespace TypedGML.Compiler.Tests.Parser;

public sealed class ParserExpressionTests
{
    [Fact]
    public void BinaryUnaryAndTernaryExpressionsParseSuccessfully() =>
        AssertParses("""
            using TypedGML.Core;
            public class OperatorExpressions {
                public void Run() {
                    number total = 1 + 2 - 3 * 4 / 5 % 6;
                    bool same = total == 1 or total != 2 and total < 3 and total > 0 and total <= 4 and total >= 0;
                    number unary = -total + ~1;
                    bool inverted = not same;
                    number selected = same ? total : unary;
                }
            }
            """);

    [Fact]
    public void AssignmentAndNullExpressionsParseSuccessfully() =>
        AssertParses("""
            using TypedGML.Core;
            public class AssignmentExpressions {
                public number Value;
                public void Run() {
                    Value = 1; Value += 1; Value -= 1; Value *= 2; Value /= 2; Value %= 2;
                    string? maybe = null;
                    string actual = maybe ?? "fallback";
                }
            }
            """);

    [Fact]
    public void NullConditionalIntrinsicAndCastExpressionsParseSuccessfully() =>
        AssertParses("""
            public class AccessNode { public number Value; public void Touch() { } }
            public class IntrinsicExpressions {
                public void Run() {
                    AccessNode? node = new AccessNode();
                    var value = node?.Value;
                    node?.Touch();
                    string typeName = typeof(AccessNode);
                    string memberName = nameof(node.Value);
                    number defaultNumber = default(number);
                    bool isNode = node is AccessNode;
                    AccessNode castNode = node as AccessNode;
                }
            }
            """);

    [Fact]
    public void LiteralLambdaCreationAndAccessExpressionsParseSuccessfully() =>
        AssertParses("""
            using TypedGML;
            using TypedGML.Collections;
            using TypedGML.GameObjects;
            using TypedGML.Core;
            public struct ExpressionPoint { public number X; public constructor(number x) { X = x; } }
            @Object("OBJ_ExpressionObject")
            public class ExpressionObject : GameObject { public ExpressionObject(number x, number y, string layer) { } }
            public class ExpressionCases {
                public event Action Changed;
                public void Run() {
                    number[] values = [1, 2, 3];
                    Dictionary<string, number> scores = {"a": 1};
                    (number x) => x * 2;
                    var point = new ExpressionPoint(1);
                    var instance = new ExpressionObject(1, 2, "Instances");
                    var chained = point.X;
                    var first = values[0];
                    Changed += () => { };
                    Changed -= () => { };
                }
            }
            """);

    [Fact]
    public void InvocationAndDelegateExpressionsParseSuccessfully() =>
        AssertParses("""
            using TypedGML;
            public class InvocationExpressions {
                public event Action Changed;
                public void Handler() { }
                public number Add(number x, number y = 1) { return x; }
                public void Run() {
                    Changed += Handler;
                    Changed -= Handler;
                    var one = Add(1);
                    var three = Add(y: 2, x: 1);
                }
            }
            """);

    private static void AssertParses(string source)
    {
        var result = CompilerFixture.Compile(source);
        result.HasErrors.Should().BeFalse(string.Join(Environment.NewLine, result.Errors.Select(error => error.Message)));
    }
}
