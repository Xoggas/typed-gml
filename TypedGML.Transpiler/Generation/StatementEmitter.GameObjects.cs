using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Generation;

public sealed partial class StatementEmitter
{
    private void EmitGameObjectCreation(string varName, TgmlNewObjectExpr newObj, TgmlClassDecl cls, GmlWriter w)
    {
        var objGml = _ctx.GmlObjectName(cls);
        var args = GetNormalizedCtorArgs(newObj).Select(_expr.Emit).ToList();
        var matchedCtor = cls.Constructors.FirstOrDefault(c => c.Params.Count == args.Count) ?? cls.Constructor;
        var baseArgs = GetNormalizedBaseArgs(matchedCtor);

        var forwardedParamNames = new HashSet<string>(
            baseArgs.OfType<TgmlIdExpr>().Select(e => e.Name),
            StringComparer.Ordinal);
        var paramToArg = new Dictionary<string, string>(StringComparer.Ordinal);

        if (matchedCtor is not null)
        {
            for (var i = 0; i < matchedCtor.Params.Count && i < args.Count; i++)
                paramToArg[matchedCtor.Params[i].Name] = args[i];
        }

        var createArgs = baseArgs.Select(baseArg =>
        {
            if (baseArg is TgmlIdExpr idExpr && paramToArg.TryGetValue(idExpr.Name, out var callerArg))
                return callerArg;

            return _expr.Emit(baseArg);
        }).ToList();

        while (createArgs.Count < 3)
            createArgs.Add("0");

        var initArgs = new List<string>();
        if (matchedCtor is not null)
        {
            for (var i = 0; i < matchedCtor.Params.Count && i < args.Count; i++)
            {
                if (!forwardedParamNames.Contains(matchedCtor.Params[i].Name))
                    initArgs.Add(args[i]);
            }
        }

        w.WriteLine($"var {varName} = instance_create_layer({string.Join(", ", createArgs)}, {objGml});");
        if (initArgs.Count > 0)
            w.WriteLine($"{objGml}_Init({varName}, {string.Join(", ", initArgs)});");
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

    private IReadOnlyList<TgmlExpression> GetNormalizedCtorArgs(TgmlNewObjectExpr expr)
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
