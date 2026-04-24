using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking;

public sealed partial class ExprChecker
{
    private const string DictionaryLiteralConcreteTypeMetadata = "DictionaryLiteralConcreteType";
    private const string DictionaryLiteralKeyTypeMetadata = "DictionaryLiteralKeyType";
    private const string DictionaryLiteralValueTypeMetadata = "DictionaryLiteralValueType";

    private string? InferAssign(TgmlAssignExpr expr)
    {
        CheckAssignmentTarget(expr.Target);
        if (TryResolveAssetAccess(expr.Target, out var assetName, out var assetType))
        {
            Error(expr, $"Asset reference '{assetName}' is read-only and cannot be assigned.");
            CheckExpr(expr.Value);
            return assetType;
        }

        var lt = InferLValueType(expr.Target);
        if (lt is not null)
            expr.Target.Metadata["InferredType"] = lt;

        if (lt is not null &&
            DelegateFacts.IsDelegateType(_ctx.TypeTable, lt) &&
            expr.Operator is "+=" or "-=")
        {
            CheckPropertyWrite(expr.Target, requireRead: true);
            DefaultExpressionFacts.TryApplyContextualType(expr.Value, lt);
            if (!CanConvertExpression(lt, expr.Value, allowExplicit: false, apply: true))
            {
                var delegateValueType = InferType(expr.Value);
                Error(expr, delegateValueType is not null
                    ? $"Cannot assign '{delegateValueType}' to '{lt}'."
                    : $"Cannot convert the supplied expression to delegate type '{lt}'.");
            }
            else
            {
                CheckExpr(expr.Value);
            }
        }
        else if (expr.Operator != "=")
        {
            var rt = InferType(expr.Value);
            CheckPropertyWrite(expr.Target, requireRead: true);
            if (lt is not null && rt is not null)
                InferBinary(new TgmlBinaryExpr
                    { Left = expr.Target, Operator = expr.Operator[..^1], Right = expr.Value, Line = expr.Line, Column = expr.Column });
        }
        else
        {
            if (lt is not null)
            {
                DefaultExpressionFacts.TryApplyContextualType(expr.Value, lt);
                if (expr.Value is TgmlNewImplicitExpr niAssign)
                    niAssign.Metadata["InferredType"] = lt;
            }
            var rt = InferType(expr.Value);
            CheckPropertyWrite(expr.Target, requireRead: false);
            if (lt is not null && !CanConvertExpression(lt, expr.Value, allowExplicit: false, apply: true))
                Error(expr, rt is not null
                    ? $"Cannot assign '{rt}' to '{lt}'."
                    : $"Cannot convert the supplied expression to '{lt}'.");
            else
                CheckExpr(expr.Value);
        }

        return lt;
    }

    // -- Visitor stubs (recurse, return null) ---------------------------------

    private string? VisitArrayInit(TgmlArrayInitExpr expr)
    {
        foreach (var el in expr.Elements) CheckExpr(el);
        return null;
    }

    private string? VisitDictionaryInit(TgmlDictionaryInitExpr expr)
    {
        var contextualType = DefaultExpressionFacts.GetEffectiveDictionaryTypeName(expr);
        if (contextualType is not null)
        {
            if (!TryResolveDictionaryType(contextualType, out var keyType, out var valueType, out var concreteType))
            {
                foreach (var entry in expr.Entries)
                {
                    CheckExpr(entry.Key);
                    CheckExpr(entry.Value);
                }

                Error(expr,
                    "Dictionary literals require a target type compatible with 'System.Collections.Dictionary<TKey, TValue>' or its dictionary interfaces.");
                return "any";
            }

            ApplyDictionaryLiteralTypes(expr, keyType, valueType, concreteType);
            ValidateDictionaryEntries(expr, keyType, valueType);
            return contextualType;
        }

        foreach (var entry in expr.Entries)
        {
            CheckExpr(entry.Key);
            CheckExpr(entry.Value);
        }

        if (!TryInferDictionaryType(expr, out var inferredType, out var inferredKeyType, out var inferredValueType, out var inferredConcreteType))
        {
            Error(expr,
                "Cannot infer dictionary literal type without a target type. Specify 'Dictionary<TKey, TValue>' or assign the literal to one.");
            return "any";
        }

        ApplyDictionaryLiteralTypes(expr, inferredKeyType, inferredValueType, inferredConcreteType);
        ValidateDictionaryEntries(expr, inferredKeyType, inferredValueType);
        return inferredType;
    }

    private void ValidateDictionaryEntries(TgmlDictionaryInitExpr expr, string keyType, string valueType)
    {
        foreach (var entry in expr.Entries)
        {
            DefaultExpressionFacts.TryApplyContextualType(entry.Key, keyType);
            if (!CanConvertExpression(keyType, entry.Key, allowExplicit: false, apply: true))
            {
                var actualKeyType = InferType(entry.Key);
                Error(entry.Key, actualKeyType is not null
                    ? $"Cannot use dictionary key of type '{actualKeyType}' where '{keyType}' is required."
                    : $"Cannot convert dictionary key to '{keyType}'.");
            }

            DefaultExpressionFacts.TryApplyContextualType(entry.Value, valueType);
            if (!CanConvertExpression(valueType, entry.Value, allowExplicit: false, apply: true))
            {
                var actualValueType = InferType(entry.Value);
                Error(entry.Value, actualValueType is not null
                    ? $"Cannot use dictionary value of type '{actualValueType}' where '{valueType}' is required."
                    : $"Cannot convert dictionary value to '{valueType}'.");
            }
        }
    }

    private void ApplyDictionaryLiteralTypes(TgmlDictionaryInitExpr expr, string keyType, string valueType, string concreteType)
    {
        expr.Metadata[DictionaryLiteralKeyTypeMetadata] = keyType;
        expr.Metadata[DictionaryLiteralValueTypeMetadata] = valueType;
        expr.Metadata[DictionaryLiteralConcreteTypeMetadata] = concreteType;
    }

    private static bool TryResolveDictionaryType(
        string describedType,
        out string keyType,
        out string valueType,
        out string concreteType)
    {
        var (baseName, typeArgs) = DelegateFacts.SplitDescribedType(describedType);
        if (typeArgs.Count != 2)
        {
            keyType = string.Empty;
            valueType = string.Empty;
            concreteType = string.Empty;
            return false;
        }

        if (baseName is not ("Dictionary" or "System.Collections.Dictionary" or
                             "IDictionary" or "System.Collections.IDictionary" or
                             "IReadOnlyDictionary" or "System.Collections.IReadOnlyDictionary"))
        {
            keyType = string.Empty;
            valueType = string.Empty;
            concreteType = string.Empty;
            return false;
        }

        keyType = typeArgs[0];
        valueType = typeArgs[1];
        concreteType = $"System.Collections.Dictionary<{keyType}, {valueType}>";
        return true;
    }

    private bool TryInferDictionaryType(
        TgmlDictionaryInitExpr expr,
        out string inferredType,
        out string keyType,
        out string valueType,
        out string concreteType)
    {
        inferredType = string.Empty;
        keyType = string.Empty;
        valueType = string.Empty;
        concreteType = string.Empty;

        if (expr.Entries.Count == 0)
            return false;

        string? mergedKeyType = null;
        string? mergedValueType = null;

        foreach (var entry in expr.Entries)
        {
            var entryKeyType = InferType(entry.Key);
            var entryValueType = InferType(entry.Value);
            if (entryKeyType is null || entryValueType is null)
                return false;

            mergedKeyType = MergeDictionaryLiteralType(mergedKeyType, entryKeyType);
            mergedValueType = MergeDictionaryLiteralType(mergedValueType, entryValueType);
            if (mergedKeyType is null || mergedValueType is null)
                return false;
        }

        keyType = mergedKeyType!;
        valueType = mergedValueType!;
        inferredType = $"System.Collections.Dictionary<{keyType}, {valueType}>";
        concreteType = inferredType;
        return true;
    }

    private string? MergeDictionaryLiteralType(string? current, string next)
    {
        if (current is null || current == "null")
            return next;
        if (next == "null")
            return current;
        if (current == next)
            return current;
        if (BuiltinTypeFacts.IsNumeric(current) && BuiltinTypeFacts.IsNumeric(next))
            return "number";
        if (CanAssignTypeToType(current, next))
            return current;
        if (CanAssignTypeToType(next, current))
            return next;

        return null;
    }

}
