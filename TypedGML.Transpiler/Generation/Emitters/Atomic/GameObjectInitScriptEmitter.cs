using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Generation.Emitters.Atomic;

/// <summary>
///     Emits the auxiliary init script used by GameMaker objects when constructor parameters
///     remain after base-constructor forwarding.
/// </summary>
internal static class GameObjectInitScriptEmitter
{
    /// <summary>
    ///     Emits <c>Scripts/{initScriptName}/{initScriptName}.gml</c> for <paramref name="ctor"/>
    ///     when it has non-forwarded parameters that must be applied after instance creation.
    ///     Returns <c>null</c> when no init script is required.
    /// </summary>
    public static GeneratedFile? TryEmit(
        TgmlConstructorDecl ctor,
        string initScriptName,
        GenerationContext ctx)
    {
        if (ctor.Body is null || ctor.Params.Count == 0)
            return null;

        var baseArgNames = new HashSet<string>(
            GetNormalizedBaseArgs(ctor).OfType<TgmlIdExpr>().Select(e => e.Name),
            StringComparer.Ordinal);
        var extraParams = ctor.Params.Where(p => !baseArgNames.Contains(p.Name)).ToList();
        if (extraParams.Count == 0)
            return null;

        var writer   = new GmlWriter();
        var stmtEmit = new StatementEmitter(ctx);
        var paramStr = string.Join(", ", new[] { "inst" }.Concat(extraParams.Select(p => p.Name)));

        writer.WriteLine($"function {initScriptName}({paramStr})");
        writer.OpenBrace();

        var previousAlias = ctx.SelfAlias;
        try
        {
            ctx.SelfAlias = "inst";
            stmtEmit.EmitBlock(ctor.Body, writer);
        }
        finally
        {
            ctx.SelfAlias = previousAlias;
        }

        writer.CloseBrace();
        return new GeneratedFile($"Scripts/{initScriptName}/{initScriptName}.gml", writer.ToString());
    }

    private static IReadOnlyList<TgmlExpression> GetNormalizedBaseArgs(TgmlConstructorDecl ctor)
    {
        if (ctor.Metadata.TryGetValue("NormalizedBaseArgs", out var value) &&
            value is List<TgmlExpression> normalized)
            return normalized;

        return ctor.BaseArgs?.Select(a => a.Value).ToList() ?? [];
    }
}
