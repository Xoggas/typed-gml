using FluentAssertions;
using TypedGML.Compiler.Tests.Helpers;

namespace TypedGML.Compiler.Tests.Parser;

public sealed class ParserBasicTests
{
    [Fact]
    public void EmptyFileParsesSuccessfully() =>
        AssertParses(string.Empty);

    [Fact]
    public void FileScopedNamespaceDeclarationParsesSuccessfully() =>
        AssertParses("namespace Parser.Basic;");

    [Fact]
    public void BracedNamespaceDeclarationParsesSuccessfully() =>
        AssertParses("namespace Parser.Basic { public class NamespacedType { } }");

    [Fact]
    public void UsingDirectivesParseSuccessfully() =>
        AssertParses("using TypedGML.Collections; using Collections = TypedGML.Collections; public class UsingTarget { }");

    [Fact]
    public void TopLevelFunctionDeclarationParsesSuccessfully() =>
        AssertParses("public number AddTopLevel(number a, number b) { return a; }");

    private static void AssertParses(string source)
    {
        var result = CompilerFixture.Compile(source);
        result.HasErrors.Should().BeFalse(string.Join(Environment.NewLine, result.Errors.Select(error => error.Message)));
    }
}
