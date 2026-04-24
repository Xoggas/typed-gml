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

    private bool TryEmitPropertyAccess(TgmlExpression target, string propertyName, out string emitted)
    {
        emitted = string.Empty;

        var resolvedProperty = _ctx.FindResolvedProperty(target, propertyName);
        if (resolvedProperty?.Property.Getter is null)
            return false;

        if (TryEmitInlinedPrimitivePropertyGetter(target, resolvedProperty, out emitted))
            return true;

        emitted = $"{BuildIndexerQualifier(target)}get_{propertyName}()";
        return true;
    }

    private bool TryEmitInlinedPrimitivePropertyGetter(
        TgmlExpression target,
        PropertyAccessHelper.ResolvedProperty resolvedProperty,
        out string emitted)
    {
        emitted = string.Empty;

        if (!IsPrimitiveWrapperAccess(target, resolvedProperty.DeclaringType))
            return false;

        var getterBody = resolvedProperty.Property.Getter?.Body;
        if (getterBody?.Statements is not [TgmlReturnStmt { Value: TgmlFuncCallExpr call }])
            return false;

        var helper = FindMethod(resolvedProperty.DeclaringType, call.FunctionName);
        if (!TryGetNativeCallName(helper, out var nativeCallName))
            return false;

        emitted = $"{nativeCallName}({string.Join(", ", call.Args.Select(arg => EmitPrimitiveGetterArgument(arg.Value, target)))})";
        return true;
    }

    private bool TryEmitInlinedPrimitiveMethodCall(
        TgmlMethodCallExpr methodCall,
        IReadOnlyList<TgmlExpression> normalizedArgs,
        out string emitted)
    {
        emitted = string.Empty;

        if (!methodCall.Metadata.TryGetValue("ResolvedMethod", out var methodValue) ||
            methodValue is not TgmlMethodDecl method ||
            !methodCall.Metadata.TryGetValue("ResolvedMethodOwner", out var ownerValue) ||
            ownerValue is not TgmlTypeDecl declaringType ||
            !IsPrimitiveWrapperAccess(methodCall.Target, declaringType))
        {
            return false;
        }

        if (method.Body?.Statements is not [TgmlReturnStmt { Value: TgmlFuncCallExpr call }])
            return false;

        var helper = FindMethod(declaringType, call.FunctionName);
        if (!TryGetNativeCallName(helper, out var nativeCallName))
            return false;

        var argumentMap = method.Params
            .Select((param, index) => (param.Name, Argument: index < normalizedArgs.Count ? normalizedArgs[index] : null))
            .Where(entry => entry.Argument is not null)
            .ToDictionary(entry => entry.Name, entry => entry.Argument!, StringComparer.Ordinal);

        emitted = $"{nativeCallName}({string.Join(", ", call.Args.Select(arg => EmitPrimitiveWrapperArgument(arg.Value, methodCall.Target, argumentMap)))})";
        return true;
    }

    private string EmitPrimitiveGetterArgument(TgmlExpression expression, TgmlExpression target)
        => expression is TgmlIdExpr { Name: "this" or "self" } ? Emit(target) : Emit(expression);

    private string EmitPrimitiveWrapperArgument(
        TgmlExpression expression,
        TgmlExpression target,
        IReadOnlyDictionary<string, TgmlExpression> argumentMap)
    {
        if (expression is TgmlIdExpr { Name: "this" or "self" })
            return Emit(target);

        if (expression is TgmlIdExpr id && argumentMap.TryGetValue(id.Name, out var argument))
            return Emit(argument);

        return Emit(expression);
    }

    private static TgmlMethodDecl? FindMethod(TgmlTypeDecl declaringType, string methodName)
        => declaringType switch
        {
            TgmlClassDecl cls => cls.Methods.FirstOrDefault(method => method.Name == methodName),
            TgmlStructDecl str => str.Methods.FirstOrDefault(method => method.Name == methodName),
            _ => null
        };

    private static bool TryGetNativeCallName(TgmlMethodDecl? method, out string nativeCallName)
    {
        nativeCallName = string.Empty;
        if (method is null)
            return false;

        if (method.Metadata.TryGetValue("NativeCallName", out var metadataValue) &&
            metadataValue is string metadataName &&
            !string.IsNullOrWhiteSpace(metadataName))
        {
            nativeCallName = metadataName;
            return true;
        }

        var decoratorName = method.GetDecorator("NativeCall")?.GetFirstStringArg();
        if (string.IsNullOrWhiteSpace(decoratorName))
            return false;

        nativeCallName = decoratorName;
        return true;
    }

    private static bool IsPrimitiveWrapperAccess(TgmlExpression target, TgmlTypeDecl declaringType)
    {
        if (target.Metadata.TryGetValue("InferredType", out var inferredType) is false ||
            inferredType is not string typeName)
        {
            return false;
        }

        var declaringName = declaringType.QualifiedName ?? declaringType.Name;
        return (typeName, declaringName) is
            ("string", "System.String") or
            ("number", "System.Number") or
            ("int", "System.Number") or
            ("real", "System.Number") or
            ("bool", "System.Bool");
    }
}
