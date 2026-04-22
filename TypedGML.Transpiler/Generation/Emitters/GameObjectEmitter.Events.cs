using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Generation.Emitters;

public sealed partial class GameObjectEmitter
{
    private static bool IsNativeEvent(TgmlClassDecl owner, TgmlMethodDecl method, GenerationContext ctx)
        => TryGetNativeEventName(owner, method, ctx, out _);

    private static bool TryGetNativeEventName(
        TgmlClassDecl owner,
        TgmlMethodDecl method,
        GenerationContext ctx,
        out string eventName)
    {
        eventName = string.Empty;

        if (method.Metadata.TryGetValue("NativeEventName", out var ev) && ev is string named)
        {
            eventName = named;
            return true;
        }

        if (method.IsOverride && KnownEvents.Contains(method.Name))
        {
            eventName = method.Name;
            return true;
        }

        if (method.IsOverride &&
            ctx.TryFindBaseMethod(owner, method.Name, out _, out var baseMethod) &&
            TryGetNativeEventName(baseMethod, out eventName))
        {
            return true;
        }

        return false;
    }

    private static bool TryGetNativeEventName(TgmlMethodDecl method, out string eventName)
    {
        if (method.Metadata.TryGetValue("NativeEventName", out var ev) && ev is string named)
        {
            eventName = named;
            return true;
        }

        if (method.IsOverride && KnownEvents.Contains(method.Name))
        {
            eventName = method.Name;
            return true;
        }

        eventName = string.Empty;
        return false;
    }

    private static bool TryGetCreateEventMethod(TgmlClassDecl owner, GenerationContext ctx, out TgmlMethodDecl method)
    {
        method = owner.Methods.FirstOrDefault(m =>
            m.Body is not null &&
            TryGetNativeEventName(owner, m, ctx, out var eventName) &&
            string.Equals(eventName, "Create", StringComparison.OrdinalIgnoreCase))!;
        return method is not null;
    }
}
