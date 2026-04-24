using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking.Checks;

public sealed partial class TypeReferenceCheck
{
    private static IReadOnlyList<TgmlTypeParam> VisibleTypeParams(WalkContext wctx)
    {
        var visible = new List<TgmlTypeParam>(wctx.OwnerType.TypeParams);
        if (wctx.Member is TgmlMethodDecl method)
            visible.AddRange(method.TypeParams);
        return visible;
    }

    private static void ValidateTypeRef(
        TranspileContext ctx,
        TgmlFile file,
        TgmlTypeRef typeRef,
        IReadOnlyList<TgmlTypeParam> visibleTypeParams,
        int line = 0,
        int column = 0)
    {
        foreach (var arg in typeRef.TypeArgs)
            ValidateTypeRef(ctx, file, arg, visibleTypeParams, line, column);

        var name = typeRef.Name.Full;
        if (visibleTypeParams.Any(tp => tp.Name == name))
            return;
        if (BuiltinTypeFacts.IsBuiltIn(name))
            return;
        if (ctx.TypeTable.TryResolve(name, typeRef.TypeArgs.Count, out var resolvedDecl) && resolvedDecl is not null)
            return;

        ctx.AddError($"Unknown type '{name}'.", file.FileName, line, column);
    }

    private static bool ValidateImplicitLocalTypeUsage(
        TranspileContext ctx,
        TgmlFile file,
        TgmlTypeRef typeRef,
        int line,
        int column)
    {
        if (typeRef.Name.Full != "var")
            return false;

        if (typeRef.ArrayDepth > 0 || typeRef.TypeArgs.Count > 0)
        {
            ctx.AddError(
                "Implicitly typed local variables cannot use array specifiers or type arguments.",
                file.FileName,
                line,
                column);
        }

        return true;
    }
}
