using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Generation;

public sealed partial class StatementEmitter
{
    private void EmitGameObjectCreation(string varName, TgmlNewObjectExpr newObj, TgmlClassDecl cls, GmlWriter w)
    {
        var plan = GameObjectConstructionHelper.Build(newObj, cls, _ctx, _expr);
        w.WriteLine($"var {varName} = {GameObjectConstructionHelper.BuildConstructorCall(plan)};");
    }

    private bool TryEmitGameObjectExpressionStatement(TgmlExpressionStmt stmt, GmlWriter w)
    {
        if (stmt.Expression is not TgmlNewObjectExpr newObj)
            return false;

        if (!_ctx.TypeTable.TryResolve(newObj.Type.Name.Full, out var td) || td is not TgmlClassDecl objCls || !objCls.IsGameObject)
            return false;

        var plan = GameObjectConstructionHelper.Build(newObj, objCls, _ctx, _expr);
        w.WriteLine($"{GameObjectConstructionHelper.BuildConstructorCall(plan)};");
        return true;
    }

    private bool TryEmitInlinedGameObjectBaseCall(TgmlBaseCallExpr expr, GmlWriter w)
    {
        if (_ctx.CurrentType is not TgmlClassDecl { IsGameObject: true })
            return false;

        if (_ctx.CurrentMethodOwnerType is not TgmlClassDecl methodOwner)
            return false;

        if (!_ctx.TryFindBaseMethod(methodOwner, expr.MethodName, out var baseDeclaringType, out var baseMethod) ||
            baseMethod.Body is null)
        {
            return false;
        }

        var previousMethodName = _ctx.CurrentMethodName;
        var previousMethodIsOverride = _ctx.CurrentMethodIsOverride;
        var previousMethodOwnerType = _ctx.CurrentMethodOwnerType;
        var previousNativeEventName = _ctx.CurrentNativeEventName;
        var aliases = new List<KeyValuePair<string, string>>();
        var normalizedArgs = GetNormalizedBaseCallArgs(expr);

        for (var i = 0; i < Math.Min(baseMethod.Params.Count, normalizedArgs.Count); i++)
        {
            var param = baseMethod.Params[i];
            var tempName = _ctx.AllocateTempIdentifier($"base_{expr.MethodName}_{param.Name}");
            w.WriteLine($"var {tempName} = {_expr.Emit(normalizedArgs[i])};");
            aliases.Add(new KeyValuePair<string, string>(param.Name, tempName));
        }

        _ctx.PushIdentifierAliases(aliases);
        _ctx.CurrentMethodName = baseMethod.Name;
        _ctx.CurrentMethodIsOverride = baseMethod.IsOverride;
        _ctx.CurrentMethodOwnerType = baseDeclaringType;
        _ctx.CurrentNativeEventName = previousNativeEventName;

        EmitBlock(baseMethod.Body, w);

        _ctx.PopIdentifierAliases();
        _ctx.CurrentMethodName = previousMethodName;
        _ctx.CurrentMethodIsOverride = previousMethodIsOverride;
        _ctx.CurrentMethodOwnerType = previousMethodOwnerType;
        _ctx.CurrentNativeEventName = previousNativeEventName;
        return true;
    }
    private static IReadOnlyList<TgmlExpression> GetNormalizedBaseCallArgs(TgmlBaseCallExpr expr)
    {
        if (expr.Metadata.TryGetValue("NormalizedArgs", out var normalizedArgs) &&
            normalizedArgs is List<TgmlExpression> normalized)
        {
            return normalized;
        }

        return expr.Args.Select(a => a.Value).ToList();
    }
}
