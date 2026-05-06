using FluentAssertions;
using TypedGML.CLI.GameMaker;

namespace TypedGML.Compiler.Tests.Cli;

public sealed class GameMakerFolderMirrorTests
{
    [Fact]
    public void GameMakerFolderPath_DerivesFolderFromSourcePath()
    {
        var root = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "TypedGML.Tests", "tgml"));

        GameMakerFolderPath.FromSource(Path.Combine(root, "Player.tgml"), root).Should().BeNull();
        GameMakerFolderPath.FromSource(Path.Combine(root, "Player", "Player.tgml"), root).Should().Be("Player");
        GameMakerFolderPath.FromSource(Path.Combine(root, "Entities", "Enemy.tgml"), root).Should().Be("Entities");
        GameMakerFolderPath.FromSource(Path.Combine(root, "Entities", "Boss", "Boss.tgml"), root).Should().Be("Entities/Boss");
    }

    [Fact]
    public void ScriptYyWriter_EmitsDerivedFolderParent()
    {
        using var project = new TempGameMakerProject();

        new ScriptYyWriter().Write("Boss", project.Root, "Test", "Test.yyp", gmFolder: "Entities/Boss");

        File.ReadAllText(Path.Combine(project.Root, "scripts", "Boss", "Boss.yy"))
            .Should().Contain("""
              "parent":{
                "name":"Boss",
                "path":"folders/Entities/Boss.yy",
              },
            """.ReplaceLineEndings("\n"));
    }

    [Fact]
    public void ObjectYyWriter_EmitsDerivedFolderParent()
    {
        using var project = new TempGameMakerProject();

        new ObjectYyWriter().Write("Object1", project.Root, [], "Test", "Test.yyp", "Player");

        File.ReadAllText(Path.Combine(project.Root, "objects", "Object1", "Object1.yy"))
            .Should().Contain("""
              "parent":{
                "name":"Player",
                "path":"folders/Player.yy",
              },
            """.ReplaceLineEndings("\n"));
    }

    [Fact]
    public void YypUpdater_RebuildsCurrentFoldersWithNestedParents()
    {
        using var project = new TempGameMakerProject();
        var yypPath = Path.Combine(project.Root, "Test.yyp");
        File.WriteAllText(yypPath, """
            {
              "resources":[],
              "Folders":[
                {"folderPath":"folders/Custom.yy",},
                {"folderPath":"folders/Entities.yy",},
                {"folderPath":"folders/OldGenerated.yy",},
              ],
            }
            """.ReplaceLineEndings("\n"));
        var folders = FolderBuilder.Build(["Entities/Boss", "Player"]);

        new YypUpdater().Update(yypPath, ["Script1"], new HashSet<string>(StringComparer.Ordinal), ["Object1"], folders);

        File.ReadAllText(yypPath).Should().Be("""
            {
              "resources":[
                {"id":{"name":"Object1","path":"objects/Object1/Object1.yy",},},
                {"id":{"name":"Script1","path":"scripts/Script1/Script1.yy",},},
              ],
              "Folders":[
                {"folderPath":"folders/Custom.yy"},
                {"folderPath":"folders/OldGenerated.yy"},
                {"$GMFolder":"","%Name":"Entities","folderPath":"folders/Entities.yy","name":"Entities","resourceType":"GMFolder","resourceVersion":"2.0",},
                {"$GMFolder":"","%Name":"Boss","folderPath":"folders/Entities/Boss.yy","name":"Boss","resourceType":"GMFolder","resourceVersion":"2.0",},
                {"$GMFolder":"","%Name":"Player","folderPath":"folders/Player.yy","name":"Player","resourceType":"GMFolder","resourceVersion":"2.0",},
              ],
            }
            """.ReplaceLineEndings("\n"));
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
