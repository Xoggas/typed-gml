using Xunit.Sdk;

namespace TypedGML.Compiler.Tests.Infrastructure;

public static class CompilerAssert
{
    public static CompilationResult Succeeds(string source, string fileName = "main.tgml")
    {
        var result = CompilerTestDriver.Compile(source, fileName);
        Assert.True(result.Succeeded, result.StandardError);
        return result;
    }

    public static CompilationResult Succeeds(IReadOnlyDictionary<string, string> files)
    {
        var result = CompilerTestDriver.Compile(files);
        Assert.True(result.Succeeded, result.StandardError);
        return result;
    }

    public static CompilationResult FailsWith(string source, params string[] diagnostics) =>
        FailsWith(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["main.tgml"] = source
        }, diagnostics);

    public static CompilationResult FailsWith(IReadOnlyDictionary<string, string> files, params string[] diagnostics)
    {
        var result = CompilerTestDriver.Compile(files);
        Assert.False(result.Succeeded);
        ContainsAll(result.StandardError, diagnostics);
        return result;
    }

    public static void OutputContains(CompilationResult result, params string[] fragments) =>
        ContainsAll(result.AllOutput, fragments);

    public static void ErrorContains(CompilationResult result, params string[] fragments) =>
        ContainsAll(result.StandardError, fragments);

    private static void ContainsAll(string text, IEnumerable<string> fragments)
    {
        foreach (var fragment in fragments)
            if (!text.Contains(fragment, StringComparison.Ordinal))
                throw new XunitException($"Expected to find '{fragment}' in:{Environment.NewLine}{text}");
    }
}
