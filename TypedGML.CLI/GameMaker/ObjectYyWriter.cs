using System.Text;

namespace TypedGML.CLI.GameMaker;

internal class ObjectYyWriter
{
    public void Write(
        string objectName,
        string gmProjectRoot,
        IReadOnlyList<string> eventFileStems,
        string parentFolderPath)
    {
        var path = Path.Combine(gmProjectRoot, "objects", objectName, $"{objectName}.yy");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, BuildContent(objectName, eventFileStems, parentFolderPath), Encoding.UTF8);
    }

    private static string BuildContent(
        string objectName,
        IReadOnlyList<string> eventFileStems,
        string parentFolderPath) =>
        $$"""
        {
          "resourceType": "GMObject",
          "resourceVersion": "1.0",
          "name": "{{Escape(objectName)}}",
          "eventList": [
        {{BuildEvents(objectName, eventFileStems)}}  ],
          "managed": true,
          "overriddenProperties": [],
          "parent": {
            "name": "{{Escape(Path.GetFileNameWithoutExtension(parentFolderPath))}}",
            "path": "{{Escape(NormalizePath(parentFolderPath))}}",
          },
          "parentObjectId": null,
          "persistent": false,
          "physicsAngularDamping": 0.10000000149011612,
          "physicsDensity": 0.5,
          "physicsFriction": 0.20000000298023224,
          "physicsGroup": 0,
          "physicsKinematic": false,
          "physicsLinearDamping": 0.10000000149011612,
          "physicsObject": false,
          "physicsRestitution": 0.10000000149011612,
          "physicsSensor": false,
          "physicsShape": 1,
          "physicsShapePoints": null,
          "physicsStartAwake": true,
          "properties": [],
          "solid": false,
          "spriteId": null,
          "spriteMaskId": null,
          "tags": [],
          "visible": true,
        }
        """.ReplaceLineEndings("\n");

    private static string BuildEvents(string objectName, IReadOnlyList<string> eventFileStems)
    {
        if (eventFileStems.Count == 0)
            return string.Empty;

        var events = eventFileStems.Select(stem => BuildEvent(objectName, EventTypeMap.Resolve(stem)));
        return string.Join(",\n", events) + "\n";
    }

    private static string BuildEvent(string objectName, EventTypeMap.EventEntry entry) =>
        $$"""
          {
            "resourceType": "GMEvent",
            "resourceVersion": "1.0",
            "name": "",
            "isDnD": false,
            "eventNum": {{entry.Enumb}},
            "eventType": {{entry.EventType}},
            "collisionObjectId": {{CollisionObjectId(entry)}},
            "parent": {
              "name": "{{Escape(objectName)}}",
              "path": "objects/{{Escape(objectName)}}/{{Escape(objectName)}}.yy",
            },
          }
        """;

    private static string CollisionObjectId(EventTypeMap.EventEntry entry) =>
        entry.CollisionTargetName is null
            ? "null"
            : $$"""
        {
              "name": "{{Escape(entry.CollisionTargetName)}}",
              "path": "objects/{{Escape(entry.CollisionTargetName)}}/{{Escape(entry.CollisionTargetName)}}.yy",
            }
        """;

    private static string NormalizePath(string path) => path.Replace('\\', '/');

    private static string Escape(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);
}
