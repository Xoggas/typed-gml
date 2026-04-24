using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Generation;

internal static class GameObjectConstructionHelper
{
    public static GameObjectConstructionPlan Build(
        TgmlNewObjectExpr expr,
        TgmlClassDecl cls,
        GenerationContext ctx,
        ExpressionEmitter emitter)
    {
        var ctorArgs = GetNormalizedCtorArgs(expr).Select(emitter.Emit).ToList();
        var ctor = ResolveConstructor(expr, cls, ctorArgs.Count);
        var initName = BuildInitName(cls, ctor, ctx);
        return new GameObjectConstructionPlan(initName, ctorArgs);
    }

    public static string BuildConstructorCall(GameObjectConstructionPlan plan)
        => $"{plan.ConstructorName}({string.Join(", ", plan.Args)})";

    public static IReadOnlyList<string> BuildCreateArgs(
        TgmlConstructorDecl? ctor,
        IReadOnlyList<string> ctorArgs,
        ExpressionEmitter emitter)
        => BuildCreateArgsCore(ctor, ctorArgs, emitter);

    private static TgmlConstructorDecl? ResolveConstructor(TgmlNewObjectExpr expr, TgmlClassDecl cls, int argCount)
    {
        if (expr.Metadata.TryGetValue("ResolvedConstructor", out var resolvedCtor) && resolvedCtor is TgmlConstructorDecl ctor)
            return ctor;

        return cls.Constructors.FirstOrDefault(c => c.Params.Count == argCount) ?? cls.Constructor;
    }

    private static List<string> BuildCreateArgsCore(
        TgmlConstructorDecl? ctor,
        IReadOnlyList<string> ctorArgs,
        ExpressionEmitter emitter)
    {
        var paramToArg = new Dictionary<string, string>(StringComparer.Ordinal);
        if (ctor is not null)
        {
            for (var i = 0; i < Math.Min(ctor.Params.Count, ctorArgs.Count); i++)
                paramToArg[ctor.Params[i].Name] = ctorArgs[i];
        }

        var createArgs = GetNormalizedBaseArgs(ctor)
            .Select(baseArg =>
            {
                if (baseArg is TgmlIdExpr idExpr && paramToArg.TryGetValue(idExpr.Name, out var callerArg))
                    return callerArg;

                return emitter.Emit(baseArg);
            })
            .Take(3)
            .ToList();

        while (createArgs.Count < 2)
            createArgs.Add("0");
        if (createArgs.Count < 3)
            createArgs.Add("\"Instances\"");

        return createArgs;
    }

    private static string BuildInitName(TgmlClassDecl cls, TgmlConstructorDecl? ctor, GenerationContext ctx)
    {
        if (ctor is null)
            return $"{ctx.GmlObjectName(cls)}_Init_0";

        var initPlan = GameObjectInitPlanner.Build($"{ctx.GmlObjectName(cls)}_Init", cls, ctor);
        return initPlan.ScriptName;
    }

    private static IReadOnlyList<TgmlExpression> GetNormalizedCtorArgs(TgmlNewObjectExpr expr)
    {
        if (expr.Metadata.TryGetValue("NormalizedArgs", out var normalizedArgs) &&
            normalizedArgs is List<TgmlExpression> normalized)
        {
            return normalized;
        }

        return expr.Args.Select(a => a.Value).ToList();
    }

    private static IReadOnlyList<TgmlExpression> GetNormalizedBaseArgs(TgmlConstructorDecl? ctor)
    {
        if (ctor?.Metadata.TryGetValue("NormalizedBaseArgs", out var normalizedArgs) == true &&
            normalizedArgs is List<TgmlExpression> normalized)
        {
            return normalized;
        }

        return ctor?.BaseArgs?.Select(a => a.Value).ToList() ?? [];
    }
}
