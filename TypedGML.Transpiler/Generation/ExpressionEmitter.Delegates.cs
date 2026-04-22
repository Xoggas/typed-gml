using TypedGML.Transpiler.Checking;
using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Generation;

public sealed partial class ExpressionEmitter
{
    private bool TryEmitDelegateConstruction(TgmlExpression expr, out string emitted)
    {
        emitted = string.Empty;
        if (!TryGetDelegateModel(expr, out var model))
            return false;

        string? handlerExpr = null;
        if (expr is TgmlLambdaExpr lambda)
            handlerExpr = EmitLambda(lambda);
        else if (expr.Metadata.ContainsKey(DelegateMethodGroupMetadata))
            handlerExpr = EmitMethodGroupHandler(expr);

        if (handlerExpr is null)
            return false;

        emitted = $"{model.CreateHelperName}({handlerExpr})";
        return true;
    }

    private bool TryEmitDelegateAssignment(
        string? delegateType,
        string @operator,
        string targetExpr,
        string setterTarget,
        TgmlExpression valueExpr,
        out string emitted)
    {
        emitted = string.Empty;
        if (delegateType is null || !DelegateFacts.TryResolveRuntimeModel(_ctx.TypeTable, delegateType, out var model))
            return false;

        var rhs = EmitDelegateValue(delegateType, valueExpr);
        if (@operator == "=")
        {
            emitted = setterTarget == targetExpr ? $"{targetExpr} = {rhs}" : $"{setterTarget}({rhs})";
            return true;
        }
        if (@operator == "+=")
        {
            var combined = $"{model.CombineHelperName}({targetExpr}, {rhs})";
            emitted = setterTarget == targetExpr ? $"{targetExpr} = {combined}" : $"{setterTarget}({combined})";
            return true;
        }
        if (@operator == "-=")
        {
            var removed = $"{model.RemoveHelperName}({targetExpr}, {rhs})";
            emitted = setterTarget == targetExpr ? $"{targetExpr} = {removed}" : $"{setterTarget}({removed})";
            return true;
        }

        return false;
    }

    private string EmitDelegateValue(string delegateType, TgmlExpression valueExpr)
    {
        if (valueExpr is TgmlNullExpr)
            return "undefined";
        if (TryEmitDelegateConstruction(valueExpr, out var constructed))
            return constructed;
        return Emit(valueExpr);
    }

    private string EmitDelegateInvoke(TgmlExpression targetExpr, TgmlExpression ownerExpr, IReadOnlyList<TgmlExpression> args)
    {
        var targetType = ownerExpr.Metadata.TryGetValue(DelegateInvokeTypeMetadata, out var invokeType) && invokeType is string resolvedType
            ? resolvedType
            : GetInferredType(targetExpr);

        if (targetType is not null && DelegateFacts.TryResolveRuntimeModel(_ctx.TypeTable, targetType, out var model))
        {
            var emittedArgs = args.Select(Emit).ToList();
            var callArgs = emittedArgs.Count == 0 ? Emit(targetExpr) : $"{Emit(targetExpr)}, {string.Join(", ", emittedArgs)}";
            return $"{model.InvokeHelperName}({callArgs})";
        }

        return $"{Emit(targetExpr)}({string.Join(", ", args.Select(Emit))})";
    }

    private bool TryGetDelegateModel(TgmlExpression expr, out DelegateRuntimeModel model)
    {
        if (expr.Metadata.TryGetValue(ContextualDelegateTypeMetadata, out var delegateTypeValue) &&
            delegateTypeValue is string delegateType &&
            DelegateFacts.TryResolveRuntimeModel(_ctx.TypeTable, delegateType, out model))
        {
            return true;
        }

        model = null!;
        return false;
    }

    private string EmitMethodGroupHandler(TgmlExpression expr)
    {
        return expr switch
        {
            TgmlIdExpr id => $"method(self, {id.Name})",
            TgmlFieldAccessExpr fieldAccess => EmitMemberMethodGroupHandler(fieldAccess),
            _ => Emit(expr)
        };
    }

    private string EmitMemberMethodGroupHandler(TgmlFieldAccessExpr expr)
    {
        var target = Emit(expr.Target);
        return $"method({target}, {target}.{expr.FieldName})";
    }

    private static string? GetInferredType(TgmlExpression expr)
        => expr.Metadata.TryGetValue("InferredType", out var inferredType) && inferredType is string typeName ? typeName : null;

    private bool CurrentTypeHasOwnField(string name)
    {
        var currentFields = _ctx.CurrentType switch
        {
            TgmlClassDecl cls => cls.Fields,
            TgmlStructDecl str => str.Fields,
            _ => null
        };

        return currentFields?.Any(f => string.Equals(f.Name, name, StringComparison.Ordinal)) == true;
    }
}
