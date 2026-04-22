using TypedGML.Transpiler.Checking;
using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Generation;

public sealed partial class ExpressionEmitter
{
    private string EmitIdRead(TgmlIdExpr e)
    {
        if (_ctx.TryGetIdentifierAlias(e.Name, out var alias))
            return alias;
        if (e.Metadata.TryGetValue(AssetReferenceNameMetadata, out var assetReference) && assetReference is string assetName)
            return assetName;
        if (_ctx.TryResolveCurrentTypeAssetReference(e.Name, out var currentTypeAssetName))
            return currentTypeAssetName;

        if (!_ctx.IsInsideAccessorOf(e.Name) && !_ctx.IsLocalShadow(e.Name) && !CurrentTypeHasOwnField(e.Name))
        {
            var prop = _ctx.FindProperty(e.Name);
            if (prop?.Getter is not null)
                return _ctx.GetNativePropertyName(prop) ?? $"get_{e.Name}()";
        }

        return e.Name == "this" ? _ctx.SelfAlias : e.Name;
    }

    private string EmitAssign(TgmlAssignExpr e)
    {
        if (TryEmitIndexerAssignment(e, out var indexerAssignment))
            return indexerAssignment;

        if (!IsWriteTargetInOwnAccessor(e.Target))
        {
            var (propName, qualifier, prop, nativeTarget) = ExtractPropertyAccess(e.Target);
            if (propName is not null && prop?.Setter is not null)
            {
                var propertyType = DefaultExpressionFacts.DescribeType(prop.Type);
                if (nativeTarget is not null)
                    return e.Operator == "="
                        ? $"{nativeTarget} = {Emit(e.Value)}"
                        : $"{nativeTarget} = {nativeTarget} {e.Operator[..^1]} {Emit(e.Value)}";

                var setPrefix = qualifier is not null ? $"{qualifier}." : string.Empty;
                var setterCall = $"{setPrefix}set_{propName}";
                if (TryEmitDelegateAssignment(propertyType, e.Operator, $"{setPrefix}get_{propName}()", setterCall, e.Value, out var delegateSetterExpr))
                    return delegateSetterExpr;
                if (e.Operator == "=")
                    return $"{setterCall}({Emit(e.Value)})";

                var getterCall = $"{setPrefix}get_{propName}()";
                return $"{setterCall}({getterCall} {e.Operator[..^1]} {Emit(e.Value)})";
            }
        }

        if (TryEmitDelegateAssignment(GetInferredType(e.Target), e.Operator, EmitLValue(e.Target), EmitLValue(e.Target), e.Value, out var delegateExpr))
            return delegateExpr;

        return $"{EmitLValue(e.Target)} {e.Operator} {Emit(e.Value)}";
    }

    private string EmitLValue(TgmlExpression expr)
    {
        return expr switch
        {
            TgmlIdExpr e => _ctx.TryGetIdentifierAlias(e.Name, out var alias) ? alias : (e.Name == "this" ? _ctx.SelfAlias : e.Name),
            TgmlFieldAccessExpr { Target: TgmlIdExpr { Name: var targetName }, FieldName: var fieldName } when _ctx.WithAlias is { } alias && alias == targetName => fieldName,
            TgmlFieldAccessExpr e => $"{EmitLValue(e.Target)}.{e.FieldName}",
            TgmlIndexExpr e => $"{EmitLValue(e.Target)}[{Emit(e.Index)}]",
            _ => Emit(expr)
        };
    }

    private (string? propName, string? qualifier, TgmlPropertyDecl? property, string? nativeTarget) ExtractPropertyAccess(TgmlExpression target)
    {
        if (target is TgmlIdExpr id && _ctx.FindProperty(id.Name) is { } idProperty)
            return (id.Name, null, idProperty, _ctx.GetNativePropertyName(idProperty));

        if (target is TgmlFieldAccessExpr { Target: TgmlIdExpr { Name: "self" or "this" } } fa &&
            !CurrentTypeHasOwnField(fa.FieldName) &&
            _ctx.FindProperty(fa.FieldName) is { } fieldProperty)
        {
            var selfQualifier = _ctx.SelfAlias != "self" ? _ctx.SelfAlias : (string?)null;
            var nativePropName = _ctx.GetNativePropertyName(fieldProperty);
            var nativeTarget = selfQualifier is not null && nativePropName is not null ? $"{selfQualifier}.{nativePropName}" : nativePropName;
            return (fa.FieldName, selfQualifier, fieldProperty, nativeTarget);
        }

        if (target is TgmlFieldAccessExpr fieldAccess &&
            !CurrentTypeHasOwnField(fieldAccess.FieldName) &&
            _ctx.FindProperty(fieldAccess.Target, fieldAccess.FieldName) is { } resolvedProperty)
        {
            var isWithAliasTarget = _ctx.WithAlias is { } alias &&
                                    fieldAccess.Target is TgmlIdExpr { Name: var targetName } &&
                                    targetName == alias;
            var qualifier = isWithAliasTarget ? null : Emit(fieldAccess.Target);
            var nativePropertyName = _ctx.GetNativePropertyName(resolvedProperty);
            var nativeTarget = nativePropertyName is null ? null : $"{qualifier}.{nativePropertyName}";
            if (isWithAliasTarget && nativePropertyName is not null)
                nativeTarget = nativePropertyName;
            return (fieldAccess.FieldName, qualifier, resolvedProperty, nativeTarget);
        }

        return (null, null, null, null);
    }
}
