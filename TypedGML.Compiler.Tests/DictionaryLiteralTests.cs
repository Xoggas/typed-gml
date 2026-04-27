using TypedGML.Compiler.Tests.Infrastructure;

namespace TypedGML.Compiler.Tests;

public sealed class DictionaryLiteralTests
{
    [Fact]
    public void DictionaryLiteralCompilesAndEmitsBclCalls()
    {
        var result = CompilerTestDriver.Compile(
            """
            using TypedGML.Collections;

            public class Scores {
                public Dictionary<string, number> Build() {
                    Dictionary<string, number> values = {"Alice": 10, "Bob": 20};
                    return values;
                }
            }
            """);

        Assert.True(result.Succeeded, result.StandardError);
        Assert.Contains("Dictionary_create()", result.AllOutput);
        Assert.Contains("Dictionary_Add(__d, \"Alice\", 10);", result.AllOutput);
        Assert.Contains("Dictionary_Add(__d, \"Bob\", 20);", result.AllOutput);
    }

    [Fact]
    public void EmptyDictionaryLiteralEmitsConstructorOnly()
    {
        var result = CompilerTestDriver.Compile(
            """
            using TypedGML.Collections;

            public class Scores {
                public Dictionary<string, number> Build() {
                    Dictionary<string, number> values = {};
                    return values;
                }
            }
            """);

        Assert.True(result.Succeeded, result.StandardError);
        Assert.Contains("Dictionary_create()", result.AllOutput);
    }

    [Fact]
    public void DictionaryLiteralCanBeReturnedDirectly()
    {
        var result = CompilerTestDriver.Compile(
            """
            using TypedGML.Collections;

            public class Scores {
                public Dictionary<string, number> Build() {
                    return {"Alice": 10};
                }
            }
            """);

        Assert.True(result.Succeeded, result.StandardError);
        Assert.Contains("return (function()", result.AllOutput);
    }

    [Fact]
    public void DictionaryLiteralRejectsNonDictionaryTargets()
    {
        var result = CompilerTestDriver.Compile(
            """
            public class Scores {
                public void Build() {
                    number value = {"Alice": 10};
                }
            }
            """);

        Assert.False(result.Succeeded);
        Assert.Contains("TGML0038", result.StandardError);
    }
}
