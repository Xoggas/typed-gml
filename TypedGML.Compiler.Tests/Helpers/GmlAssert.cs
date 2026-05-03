using FluentAssertions;

namespace TypedGML.Compiler.Tests.Helpers;

public static class GmlAssert
{
    public static void ContainsLine(string gml, string expectedLine)
    {
        var expected = expectedLine.Trim();
        Lines(gml).Select(line => line.Trim()).Should().Contain(expected);
    }

    public static void ContainsPattern(string gml, string pattern) =>
        gml.Should().Contain(pattern);

    public static void NotContainsPattern(string gml, string pattern) =>
        gml.Should().NotContain(pattern);

    public static void HasMacro(string gml, string macroName, string value) =>
        ContainsLine(gml, $"#macro {macroName} {value}");

    public static void HasFunction(string gml, string functionName) =>
        ContainsPattern(gml, $"function {functionName}");

    public static void HasGlobalAssignment(string gml, string globalName) =>
        ContainsPattern(gml, $"global.{globalName}");

    public static void HasPragma(string gml, string functionName) =>
        ContainsPattern(gml, $"gml_pragma(\"global\", \"{functionName}()\")");

    public static void IsFormattedCorrectly(string gml)
    {
        gml.Should().NotContain("\t");
        gml.Should().NotContain("'");
        foreach (var line in Lines(gml))
        {
            line.Should().Be(line.TrimEnd());
            var leadingSpaces = line.Length - line.TrimStart(' ').Length;
            (leadingSpaces % 4).Should().Be(0);
            line.Trim().Should().NotBe("{");
        }
    }

    private static string[] Lines(string gml) =>
        gml.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
}
