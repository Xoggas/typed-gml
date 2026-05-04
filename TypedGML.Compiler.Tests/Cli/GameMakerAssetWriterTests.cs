using FluentAssertions;
using TypedGML.CLI.GameMaker;

namespace TypedGML.Compiler.Tests.Cli;

public sealed class GameMakerAssetWriterTests
{
    [Fact]
    public void ScriptYyWriter_EmitsGameMaker202414Format()
    {
        using var project = new TempGameMakerProject();

        new ScriptYyWriter().Write("Script1", project.Root, "Test", "Test.yyp");

        File.ReadAllText(Path.Combine(project.Root, "scripts", "Script1", "Script1.yy"))
            .Should().Be("""
            {
              "$GMScript":"v1",
              "%Name":"Script1",
              "isCompatibility":false,
              "isDnD":false,
              "name":"Script1",
              "parent":{
                "name":"Test",
                "path":"Test.yyp",
              },
              "resourceType":"GMScript",
              "resourceVersion":"2.0",
            }
            """.ReplaceLineEndings("\n"));
    }

    [Fact]
    public void ObjectYyWriter_EmitsGameMaker202414Format()
    {
        using var project = new TempGameMakerProject();

        new ObjectYyWriter().Write("Object1", project.Root, ["Create_0", "Step_0"], "Test", "Test.yyp");

        File.ReadAllText(Path.Combine(project.Root, "objects", "Object1", "Object1.yy"))
            .Should().Be("""
            {
              "$GMObject":"",
              "%Name":"Object1",
              "eventList":[
                {"$GMEvent":"v1","%Name":"","collisionObjectId":null,"eventNum":0,"eventType":0,"isDnD":false,"name":"","resourceType":"GMEvent","resourceVersion":"2.0",},
                {"$GMEvent":"v1","%Name":"","collisionObjectId":null,"eventNum":0,"eventType":3,"isDnD":false,"name":"","resourceType":"GMEvent","resourceVersion":"2.0",},
              ],
              "managed":true,
              "name":"Object1",
              "overriddenProperties":[],
              "parent":{
                "name":"Test",
                "path":"Test.yyp",
              },
              "parentObjectId":null,
              "persistent":false,
              "physicsAngularDamping":0.1,
              "physicsDensity":0.5,
              "physicsFriction":0.2,
              "physicsGroup":1,
              "physicsKinematic":false,
              "physicsLinearDamping":0.1,
              "physicsObject":false,
              "physicsRestitution":0.1,
              "physicsSensor":false,
              "physicsShape":1,
              "physicsShapePoints":[],
              "physicsStartAwake":true,
              "properties":[],
              "resourceType":"GMObject",
              "resourceVersion":"2.0",
              "solid":false,
              "spriteId":null,
              "spriteMaskId":null,
              "visible":true,
            }
            """.ReplaceLineEndings("\n"));
    }

    [Fact]
    public void YypUpdater_RebuildsOnlyResourcesArray()
    {
        using var project = new TempGameMakerProject();
        var yypPath = Path.Combine(project.Root, "Test.yyp");
        File.WriteAllText(yypPath, """
            {
              "AudioGroups":[
                {"name":"audiogroup_default",},
              ],
              "resources":[
                {"id":{"name":"OldScript","path":"scripts/OldScript/OldScript.yy",},},
                {"id":{"name":"Room1","path":"rooms/Room1/Room1.yy",},},
                {"id":{"name":"Object1","path":"objects/Object1/Object1.yy",},},
              ],
              "Folders":[
                {"folderPath":"folders/Custom.yy",},
              ],
            }
            """.ReplaceLineEndings("\n"));

        new YypUpdater().Update(yypPath, ["Script1"], ["Object1"], []);

        File.ReadAllText(yypPath).Should().Be("""
            {
              "AudioGroups":[
                {"name":"audiogroup_default",},
              ],
              "resources":[
                {"id":{"name":"Object1","path":"objects/Object1/Object1.yy",},},
                {"id":{"name":"OldScript","path":"scripts/OldScript/OldScript.yy",},},
                {"id":{"name":"Room1","path":"rooms/Room1/Room1.yy",},},
                {"id":{"name":"Script1","path":"scripts/Script1/Script1.yy",},},
              ],
              "Folders":[
                {"folderPath":"folders/Custom.yy",},
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
