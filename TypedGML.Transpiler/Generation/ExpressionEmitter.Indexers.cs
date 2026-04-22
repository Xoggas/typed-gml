using TypedGML.Transpiler.Checking;
using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Generation;

public sealed partial class ExpressionEmitter
{
    private bool TryEmitIndexerAccess(TgmlIndexExpr indexExpr, out string emitted)
    {
        emitted = string.Empty;
        var indexer = _ctx.FindIndexer(indexExpr.Target);
        if (indexer is null)
            return false;

        emitted = $"{BuildIndexerQualifier(indexExpr.Target)}get_Item({Emit(indexExpr.Index)})";
        return true;
    }

    private bool TryEmitIndexerAssignment(TgmlAssignExpr assignExpr, out string emitted)
    {
        emitted = string.Empty;
        if (assignExpr.Target is not TgmlIndexExpr indexExpr)
            return false;

        var indexer = _ctx.FindIndexer(indexExpr.Target);
        if (indexer?.Setter is null)
            return false;

        var qualifier = BuildIndexerQualifier(indexExpr.Target);
        var indexValue = Emit(indexExpr.Index);
        var setterCall = $"{qualifier}set_Item";
        var getterCall = $"{qualifier}get_Item({indexValue})";
        var indexerType = DefaultExpressionFacts.DescribeType(indexer.Type);

        if (TryEmitDelegateIndexerAssignment(indexerType, assignExpr.Operator, setterCall, getterCall, indexValue, assignExpr.Value, out emitted))
            return true;
        if (assignExpr.Operator == "=")
        {
            emitted = $"{setterCall}({indexValue}, {Emit(assignExpr.Value)})";
            return true;
        }

        emitted = $"{setterCall}({indexValue}, {getterCall} {assignExpr.Operator[..^1]} {Emit(assignExpr.Value)})";
        return true;
    }

    private bool TryEmitDelegateIndexerAssignment(
        string delegateType,
        string @operator,
        string setterCall,
        string getterCall,
        string indexValue,
        TgmlExpression valueExpr,
        out string emitted)
    {
        emitted = string.Empty;
        if (!DelegateFacts.TryResolveRuntimeModel(_ctx.TypeTable, delegateType, out var model))
            return false;

        var rhs = EmitDelegateValue(delegateType, valueExpr);
        if (@operator == "=")
        {
            emitted = $"{setterCall}({indexValue}, {rhs})";
            return true;
        }
        if (@operator == "+=")
        {
            emitted = $"{setterCall}({indexValue}, {model.CombineHelperName}({getterCall}, {rhs}))";
            return true;
        }
        if (@operator == "-=")
        {
            emitted = $"{setterCall}({indexValue}, {model.RemoveHelperName}({getterCall}, {rhs}))";
            return true;
        }

        return false;
    }

    private string BuildIndexerQualifier(TgmlExpression target)
    {
        if (target is TgmlIdExpr { Name: "self" or "this" })
            return string.Empty;
        if (_ctx.WithAlias is { } alias && target is TgmlIdExpr { Name: var targetName } && targetName == alias)
            return string.Empty;

        return $"{Emit(target)}.";
    }

    private bool IsWriteTargetInOwnAccessor(TgmlExpression target)
    {
        var name = target switch
        {
            TgmlIdExpr id => id.Name,
            TgmlFieldAccessExpr { Target: TgmlIdExpr { Name: "self" or "this" } } fa => fa.FieldName,
            _ => null
        };
        return name is not null && _ctx.IsInsideAccessorOf(name);
    }

    private bool TryEmitNativePropertyAccess(TgmlExpression target, string propertyName, out string emitted)
    {
        emitted = string.Empty;

        string? nativePropertyName;
        string? qualifier = null;
        if (target is TgmlIdExpr { Name: "self" or "this" })
        {
            if (CurrentTypeHasOwnField(propertyName))
                return false;
            nativePropertyName = _ctx.GetNativePropertyName(propertyName);
        }
        else
        {
            var property = _ctx.FindProperty(target, propertyName);
            nativePropertyName = _ctx.GetNativePropertyName(property);
            if (nativePropertyName is not null)
                qualifier = Emit(target);
        }

        if (nativePropertyName is null)
            return false;
        if (target is TgmlIdExpr { Name: "self" or "this" })
        {
            emitted = _ctx.SelfAlias != "self" ? $"{_ctx.SelfAlias}.{nativePropertyName}" : nativePropertyName;
            return true;
        }
        if (_ctx.WithAlias is { } alias && target is TgmlIdExpr { Name: var targetName } && targetName == alias)
        {
            emitted = nativePropertyName;
            return true;
        }
        if (qualifier is not null)
        {
            emitted = $"{qualifier}.{nativePropertyName}";
            return true;
        }

        return false;
    }
}
