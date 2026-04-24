using TypedGML.Transpiler.Checking;
using TypedGML.Transpiler.Generation.Emitters.Atomic;
using TypedGML.Transpiler.Generation.Helpers;
using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Generation.Emitters;

public sealed partial class GameObjectEmitter : ICodeEmitter
{
    private static readonly HashSet<string> KnownEvents = new(StringComparer.OrdinalIgnoreCase)
    {
        "Create", "Destroy", "Step", "Draw", "Alarm", "Collision",
        "KeyDown", "KeyUp", "KeyPressed", "MouseDown", "MouseUp",
        "MouseMoved", "BeginStep", "EndStep", "DrawGui", "PreDraw",
        "PostDraw", "RoomStart", "RoomEnd", "GameStart", "GameEnd",
        "CleanUp", "AsyncSystem", "AsyncHttp", "AsyncSaving"
    };

    public bool CanEmit(TgmlTypeDecl decl) => decl is TgmlClassDecl cls && cls.IsGameObject;

    public IEnumerable<GeneratedFile> Emit(TgmlTypeDecl decl, GenerationContext ctx)
    {
        var cls = (TgmlClassDecl)decl;
        ctx.CurrentType = cls;

        var objName = ctx.GmlObjectName(cls);
        var gmlName = cls.QualifiedName?.Replace(".", "_") ?? cls.Name;
        var createWriter = new GmlWriter();
        var eventFiles = new List<GeneratedFile>();

        createWriter.WriteLine($"// Create event for {objName}");
        createWriter.WriteLine($"__types = {{ {EmitHelpers.BuildTypesStruct(cls, ctx)} }};");

        foreach (var ancestor in ctx.GetClassAncestorChain(cls))
            EmitCreateContribution(ancestor, ctx, createWriter);
        EmitCreateContribution(cls, ctx, createWriter);

        var qualifiedTypeName = cls.QualifiedName ?? cls.Name;
        var hasUserToString = cls.Methods.Any(m => m.Name == "ToString" && m.Body is not null);
        if (!hasUserToString)
            createWriter.WriteLine($"ToString = function() {{ return \"{qualifiedTypeName}\"; }}");
        createWriter.WriteLine($"GetType = function() {{ return __TYPE_{gmlName}; }}");

        foreach (var method in cls.Methods)
        {
            if (method.IsAbstract || method.Body is null)
                continue;
            if (!TryGetNativeEventName(cls, method, ctx, out var eventName))
                continue;
            if (string.Equals(eventName, "Create", StringComparison.OrdinalIgnoreCase))
                continue;

            eventFiles.Add(GameObjectEventScriptEmitter.Emit(cls, method, eventName, objName, ctx));
        }

        yield return new GeneratedFile($"Objects/{objName}/Create_0.gml", createWriter.ToString());
        foreach (var ef in eventFiles)
            yield return ef;

        var initScriptName = $"{objName}_Init";
        var initFile = GameObjectInitScriptEmitter.Emit(cls, initScriptName, ctx);
        if (initFile is not null)
            yield return initFile;
    }

    private static void ClearMethodContext(GenerationContext ctx)
    {
        ctx.CurrentMethodName = null;
        ctx.CurrentMethodIsOverride = false;
        ctx.CurrentMethodOwnerType = null;
        ctx.CurrentNativeEventName = null;
    }
}
