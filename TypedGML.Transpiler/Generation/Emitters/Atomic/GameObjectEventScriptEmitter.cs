using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Generation.Emitters.Atomic;

/// <summary>
///     Emits a single non-Create GameMaker object event file from a TypedGML method body.
/// </summary>
internal static class GameObjectEventScriptEmitter
{
    /// <summary>
    ///     Renders <paramref name="method"/> into <c>Objects/{objName}/{eventName}_0.gml</c>.
    ///     The mutable method-related fields on <see cref="GenerationContext"/> are restored
    ///     after emission so callers can safely iterate multiple event methods.
    /// </summary>
    public static GeneratedFile Emit(
        TgmlClassDecl owner,
        TgmlMethodDecl method,
        string eventName,
        string objName,
        GenerationContext ctx)
    {
        var saved = (ctx.CurrentMethodName, ctx.CurrentMethodIsOverride,
                     ctx.CurrentMethodOwnerType, ctx.CurrentNativeEventName);

        var writer   = new GmlWriter();
        var stmtEmit = new StatementEmitter(ctx);

        try
        {
            ctx.CurrentMethodName       = method.Name;
            ctx.CurrentMethodIsOverride = method.IsOverride;
            ctx.CurrentMethodOwnerType  = owner;
            ctx.CurrentNativeEventName  = eventName;

            stmtEmit.EmitBlock(method.Body!, writer);
        }
        finally
        {
            (ctx.CurrentMethodName, ctx.CurrentMethodIsOverride,
             ctx.CurrentMethodOwnerType, ctx.CurrentNativeEventName) = saved;
        }

        return new GeneratedFile($"Objects/{objName}/{eventName}_0.gml", writer.ToString());
    }
}
