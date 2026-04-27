using TypedGML.Compiler.Tests.Infrastructure;

namespace TypedGML.Compiler.Tests;

public sealed class SmokeCompilationTests
{
    [Fact]
    public void RepositoryCorpusCompilesWithoutDiagnostics()
    {
        var root = FindSolutionRoot();
        var corpus = File.ReadAllText(Path.Combine(root, "tests", "corpus.tgml"));
        var result = CompilerTestDriver.Compile(corpus, "corpus.tgml");

        Assert.True(result.Succeeded, result.StandardError);
        Assert.NotEmpty(result.Files);
    }

    [Fact]
    public void UnknownNativeEventReportsDiagnostic()
    {
        var result = CompilerTestDriver.Compile(
            """
            using TypedGML.GameObjects;

            @Object("OBJ_Test")
            public class TestObject : GameObject {
                @NativeEvent("BogusEvent")
                public void OnBogus() { }
            }
            """);

        Assert.False(result.Succeeded);
        Assert.Contains("TGML0035", result.StandardError);
    }

    private static string FindSolutionRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "TypedGML.sln")))
                return current.FullName;
            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate the solution root.");
    }
}
