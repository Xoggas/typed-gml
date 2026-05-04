using System.Text;

namespace TypedGML.CLI.GameMaker;

internal sealed class ObjectYyWriter
{
    public void Write(
        string objectName,
        string gmProjectRoot,
        IReadOnlyList<string> eventFileStems,
        string projectName,
        string yypFileName)
    {
        var path = Path.Combine(gmProjectRoot, "objects", objectName, $"{objectName}.yy");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, BuildContent(objectName, eventFileStems, projectName, yypFileName), new UTF8Encoding(false));
    }

    private static string BuildContent(
        string objectName,
        IReadOnlyList<string> eventFileStems,
        string projectName,
        string yypFileName) =>
        $$"""
        {
          "$GMObject":"",
          "%Name":"{{Escape(objectName)}}",
          "eventList":{{BuildEvents(eventFileStems)}},
          "managed":true,
          "name":"{{Escape(objectName)}}",
          "overriddenProperties":[],
          "parent":{
            "name":"{{Escape(projectName)}}",
            "path":"{{Escape(NormalizePath(yypFileName))}}",
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
        """.ReplaceLineEndings("\n");

    private static string BuildEvents(IReadOnlyList<string> eventFileStems)
    {
        if (eventFileStems.Count == 0)
            return "[]";

        var events = eventFileStems
            .Select(stem => $"    {BuildEvent(EventTypeMap.Resolve(stem))}");
        return "[\n" + string.Join("\n", events) + "\n  ]";
    }

    private static string BuildEvent(EventTypeMap.EventEntry entry) =>
        $$"""{"$GMEvent":"v1","%Name":"","collisionObjectId":{{CollisionObjectId(entry)}},"eventNum":{{entry.EventNum}},"eventType":{{entry.EventType}},"isDnD":false,"name":"","resourceType":"GMEvent","resourceVersion":"2.0",},""";

    private static string CollisionObjectId(EventTypeMap.EventEntry entry) =>
        entry.CollisionTargetName is null
            ? "null"
            : $$"""{"name":"{{Escape(entry.CollisionTargetName)}}","path":"objects/{{Escape(entry.CollisionTargetName)}}/{{Escape(entry.CollisionTargetName)}}.yy",}""";

    private static string NormalizePath(string path) => path.Replace('\\', '/');

    private static string Escape(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);
}
