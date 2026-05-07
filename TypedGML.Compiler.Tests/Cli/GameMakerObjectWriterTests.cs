using FluentAssertions;
using TypedGML.CLI;

namespace TypedGML.Compiler.Tests.Cli;

public sealed class GameMakerObjectWriterTests
{
    [Fact]
    public void GameMakerProjectWriter_WritesObjectWithoutEvents()
    {
        using var project = new TempGameMakerProject();
        var yypPath = Path.Combine(project.Root, "Test.yyp");
        var compileResult = CliCompileResult.FromOutput(
            new Dictionary<string, string>(),
            Path.Combine(project.Root, "compiled"),
            new Dictionary<string, string>(StringComparer.Ordinal),
            new Dictionary<string, string>(StringComparer.Ordinal),
            new HashSet<string>(StringComparer.Ordinal),
            ["obj_Player"]);

        new GameMakerProjectWriter().Write(compileResult, yypPath, project.Root);

        var objectDir = Path.Combine(project.Root, "objects", "obj_Player");
        File.Exists(Path.Combine(objectDir, "obj_Player.yy")).Should().BeTrue();
        Directory.EnumerateFiles(objectDir, "*.gml").Should().BeEmpty();
        File.ReadAllText(Path.Combine(objectDir, "obj_Player.yy"))
            .Should().Contain("\"eventList\":[]");
    }

    [Fact]
    public void GameMakerProjectWriter_RemovesStaleObjectEvents()
    {
        using var project = new TempGameMakerProject();
        var yypPath = Path.Combine(project.Root, "Test.yyp");
        var outputRoot = Path.Combine(project.Root, "compiled");
        var output = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [Path.Combine(outputRoot, "objects", "obj_Player", "Step_0.gml")] = "step body",
        };
        var compileResult = CliCompileResult.FromOutput(
            output,
            outputRoot,
            new Dictionary<string, string>(StringComparer.Ordinal),
            new Dictionary<string, string>(StringComparer.Ordinal),
            new HashSet<string>(StringComparer.Ordinal),
            ["obj_Player"]);

        var objectDir = Path.Combine(project.Root, "objects", "obj_Player");
        Directory.CreateDirectory(objectDir);
        File.WriteAllText(Path.Combine(objectDir, "Create_0.gml"), "stale");
        File.WriteAllText(Path.Combine(objectDir, "Step_0.gml"), "old");

        new GameMakerProjectWriter().Write(compileResult, yypPath, project.Root);

        File.Exists(Path.Combine(objectDir, "Create_0.gml")).Should().BeFalse();
        File.ReadAllText(Path.Combine(objectDir, "Step_0.gml")).Should().Be("step body");
    }

    private sealed class TempGameMakerProject : IDisposable
    {
        public string Root { get; } = Path.Combine(Path.GetTempPath(), "TypedGML.Tests", Guid.NewGuid().ToString("N"));

        public TempGameMakerProject()
        {
            Directory.CreateDirectory(Root);
        }

        public void Dispose()
        {
            if (Directory.Exists(Root))
                Directory.Delete(Root, true);
        }
    }
}
