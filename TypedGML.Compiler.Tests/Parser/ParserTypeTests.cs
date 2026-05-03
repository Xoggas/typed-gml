using FluentAssertions;
using TypedGML.Compiler.Tests.Helpers;

namespace TypedGML.Compiler.Tests.Parser;

public sealed class ParserTypeTests
{
    [Fact]
    public void ClassDeclarationFormsParseSuccessfully() =>
        AssertParses("""
            public interface IParserTag { }
            public class PlainClass { }
            public class BaseClass { }
            public class ChildClass : BaseClass { }
            public class InterfaceClass : IParserTag { }
            public class GenericClass<T> { public T Value; }
            """);

    [Fact]
    public void StructDeclarationParsesSuccessfully() =>
        AssertParses("public struct ParserPoint { public number X; public number Y; }");

    [Fact]
    public void InterfaceDeclarationMembersParseSuccessfully() =>
        AssertParses("""
            using TypedGML;
            public interface IParserShape {
                number Area { get; }
                number Size { get; set; }
                number this[number index] { get; }
                event Action Changed;
                string Describe(number scale);
            }
            """);

    [Fact]
    public void EnumDeclarationValuesParseSuccessfully() =>
        AssertParses("public enum ParserDirection { North, East = 2, South, West = 8 }");

    [Fact]
    public void DelegateDeclarationFormsParseSuccessfully() =>
        AssertParses("""
            public delegate void ParserCallback();
            public delegate TResult ParserMapper<T, TResult>(T value);
            """);

    private static void AssertParses(string source)
    {
        var result = CompilerFixture.Compile(source);
        result.HasErrors.Should().BeFalse(string.Join(Environment.NewLine, result.Errors.Select(error => error.Message)));
    }
}
