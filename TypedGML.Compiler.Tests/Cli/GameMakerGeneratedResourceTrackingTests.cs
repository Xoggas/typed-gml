using FluentAssertions;
using TypedGML.CLI;

namespace TypedGML.Compiler.Tests.Cli;

public sealed class GameMakerGeneratedResourceTrackingTests
{
    [Fact]
    public void GameMakerProjectWriter_RemovesResourcesMissingFromNextBuild()
    {
        using var project = new TempGameMakerProject();
        var yypPath = Path.Combine(project.Root, "Test.yyp");
        var tgmlRoot = Path.Combine(project.Root, "tgml");
        var sourcePath = Path.Combine(tgmlRoot, "Gameplay", "Player.tgml");
        Directory.CreateDirectory(Path.Combine(project.Root, "scripts", "HandMade"));
        File.WriteAllText(Path.Combine(project.Root, "scripts", "HandMade", "HandMade.yy"), string.Empty);
        File.WriteAllText(yypPath, """
            {
              "resources":[
                {"id":{"name":"HandMade","path":"scripts/HandMade/HandMade.yy",},},
              ],
              "Folders":[
                {"folderPath":"folders/Custom.yy",},
              ],
            }
            """.ReplaceLineEndings("\n"));

        new GameMakerProjectWriter().Write(
            BuildResult(project.Root, "compiled1", sourcePath),
            yypPath,
            tgmlRoot);
        new GameMakerProjectWriter().Write(
            EmptyResult(project.Root, "compiled2"),
            yypPath,
            tgmlRoot);

        Directory.Exists(Path.Combine(project.Root, "scripts", "Player")).Should().BeFalse();
        Directory.Exists(Path.Combine(project.Root, "objects", "obj_Player")).Should().BeFalse();
        Directory.Exists(Path.Combine(project.Root, "scripts", "HandMade")).Should().BeTrue();
        var yyp = File.ReadAllText(yypPath);
        yyp.Should().Contain("HandMade");
        yyp.Should().Contain("folders/Custom.yy");
        yyp.Should().NotContain("Player");
        yyp.Should().NotContain("obj_Player");
        yyp.Should().NotContain("folders/Gameplay.yy");
    }

    private static CliCompileResult BuildResult(string root, string outputFolder, string sourcePath)
    {
        var outputRoot = Path.Combine(root, outputFolder);
        var output = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [Path.Combine(outputRoot, "scripts", "Player.gml")] = "function Player() { }",
            [Path.Combine(outputRoot, "objects", "obj_Player", "Create_0.gml")] = "init",
        };
        var sourcePaths = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Player"] = sourcePath,
            ["obj_Player"] = sourcePath,
        };

        return CliCompileResult.FromOutput(
            output,
            outputRoot,
            new Dictionary<string, string>(StringComparer.Ordinal),
            sourcePaths,
            new HashSet<string>(StringComparer.Ordinal),
            ["obj_Player"]);
    }

    private static CliCompileResult EmptyResult(string root, string outputFolder)
    {
        return CliCompileResult.FromOutput(
            new Dictionary<string, string>(StringComparer.Ordinal),
            Path.Combine(root, outputFolder),
            new Dictionary<string, string>(StringComparer.Ordinal),
            new Dictionary<string, string>(StringComparer.Ordinal),
            new HashSet<string>(StringComparer.Ordinal));
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
