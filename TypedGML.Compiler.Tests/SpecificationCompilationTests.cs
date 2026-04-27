using TypedGML.Compiler.Tests.Infrastructure;

namespace TypedGML.Compiler.Tests;

public sealed class SpecificationCompilationTests
{
    [Fact]
    public void UsingAliasResolvesAcrossFiles()
    {
        var result = CompilerAssert.Succeeds(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Entities/Player.tgml"] =
            """
            namespace Game.Entities;

            public class Player {
            }
            """,
            ["Factory.tgml"] =
            """
            using P = Game.Entities.Player;

            public class Factory {
                public P Make() {
                    return new P();
                }
            }
            """
        });

        Assert.Contains("function Factory_create()", result.AllOutput, StringComparison.Ordinal);
    }

    [Fact]
    public void InterfacesStructsAndOverridesCompile()
    {
        var result = CompilerAssert.Succeeds(
            """
            public interface IDescribable {
                string Describe();
            }

            public abstract class Shape {
                public abstract string Describe();
            }

            public class Circle : Shape, IDescribable {
                public override string Describe() {
                    return "Circle";
                }
            }

            public struct Point : IDescribable {
                public string Describe() {
                    return "Point";
                }
            }
            """);

        CompilerAssert.OutputContains(result,
            "function Circle_Describe",
            "function Point_Describe");
    }

    [Fact]
    public void PropertiesIndexersDefaultParametersAndNamedArgumentsCompile()
    {
        var result = CompilerAssert.Succeeds(
            """
            public class Box {
                private number[] _items = [1, 2];

                public number Value { get; set; }

                public number this[number index] {
                    get { return _items[index]; }
                    set { _items[index] = value; }
                }

                public number Sum(number x, number y = 2) {
                    return y;
                }

                public number Run() {
                    Value = Sum(x: 1);
                    return Value;
                }
            }
            """);

        CompilerAssert.OutputContains(result,
            "function Box_get_Value",
            "function Box_set_Value",
            "function Box_get_indexer");
    }

    [Fact]
    public void GenericConstraintSatisfiedCaseCompiles()
    {
        var result = CompilerAssert.Succeeds(
            """
            public interface IToken {
                string Name();
            }

            public struct Token : IToken {
                public string Name() {
                    return "ok";
                }
            }

            public class Box<T : IToken> {
                public T Value;
            }

            public class Factory {
                public Box<Token> Make() {
                    return new Box<Token>();
                }
            }
            """);

        Assert.Contains("function Box_create()", result.AllOutput, StringComparison.Ordinal);
    }

    [Fact]
    public void XmlDocCommentsAndCustomDecoratorUsageCompile()
    {
        var result = CompilerAssert.Succeeds(
            """
            /// <summary>Decorated sample.</summary>
            @MyCustomDecorator("demo")
            public class Sample {
                /// <summary>Returns a constant.</summary>
                public number Value() {
                    return 1;
                }
            }
            """);

        Assert.Contains("function Sample_Value", result.AllOutput, StringComparison.Ordinal);
    }

    [Fact]
    public void TopLevelFunctionsCompileAndEmitScriptsPerSpecification()
    {
        var result = CompilerAssert.Succeeds(
            """
            public number Add(number a, number b) {
                return a;
            }
            """);

        var script = result.GetRequiredFile("scripts/Add.gml");
        Assert.Contains("function Add(a, b)", script, StringComparison.Ordinal);
    }
}
