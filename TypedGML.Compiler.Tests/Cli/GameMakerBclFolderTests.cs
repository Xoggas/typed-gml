using FluentAssertions;
using TypedGML.CLI.GameMaker;

namespace TypedGML.Compiler.Tests.Cli;

public sealed class GameMakerBclFolderTests
{
    [Fact]
    public void ScriptYyWriter_EmitsBclParent()
    {
        using var project = new TempGameMakerProject();

        new ScriptYyWriter().Write("TypedGML_Core_Object", project.Root, "Test", "Test.yyp", true);

        File.ReadAllText(Path.Combine(project.Root, "scripts", "TypedGML_Core_Object", "TypedGML_Core_Object.yy"))
            .Should().Be("""
            {
              "$GMScript":"v1",
              "%Name":"TypedGML_Core_Object",
              "isCompatibility":false,
              "isDnD":false,
              "name":"TypedGML_Core_Object",
              "parent":{
                "name":"BCL",
                "path":"folders/BCL.yy",
              },
              "resourceType":"GMScript",
              "resourceVersion":"2.0",
            }
            """.ReplaceLineEndings("\n"));
    }

    [Fact]
    public void YypUpdater_RebuildsCurrentBclFolder()
    {
        using var project = new TempGameMakerProject();
        var yypPath = Path.Combine(project.Root, "Test.yyp");
        File.WriteAllText(yypPath, """
            {
              "resources":[],
              "Folders":[
                {"folderPath":"folders/Custom.yy",},
                {"folderPath":"folders/BCL.yy",},
                {"folderPath":"folders/TypedGML/Old.yy",},
              ],
            }
            """.ReplaceLineEndings("\n"));

        new YypUpdater().Update(
            yypPath,
            ["TypedGML_Core_Object"],
            new HashSet<string>(["TypedGML_Core_Object"], StringComparer.Ordinal),
            [],
            []);

        File.ReadAllText(yypPath).Should().Be("""
            {
              "resources":[
                {"id":{"name":"TypedGML_Core_Object","path":"scripts/TypedGML_Core_Object/TypedGML_Core_Object.yy",},},
              ],
              "Folders":[
                {"folderPath":"folders/Custom.yy"},
                {"folderPath":"folders/TypedGML/Old.yy"},
                {
                  "$GMFolder":"",
                  "%Name":"BCL",
                  "folderPath":"folders/BCL.yy",
                  "name":"BCL",
                  "order":0,
                  "parent":{
                    "name":"Test",
                    "path":"Test.yyp",
                  },
                  "resourceType":"GMFolder",
                  "resourceVersion":"2.0",
                },
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
