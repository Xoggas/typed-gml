using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking;

public sealed partial class ExprChecker
{
    private string? InferIdType(TgmlIdExpr id)
    {
        // 1. Local variable / parameter in scope
        if (Symbols.TryResolve(id.Name, out var typeRef) && typeRef is not null)
            return DefaultExpressionFacts.DescribeType(typeRef);

        // 1b. Instance self-reference
        if (id.Name is "self" or "this")
            return _owner?.QualifiedName ?? _owner?.Name;

        // 2. Property on the owning type / base types
        var prop = ResolvePropertyOnCurrentType(id.Name);
        if (prop is not null)
        {
            EnsureReadable(prop, id);
            if (prop.Property.IsStatic && AssetFacts.TryGetAssetName(prop.Property, out var assetName))
                id.Metadata[AssetReferenceNameMetadata] = assetName;
            return DefaultExpressionFacts.DescribeType(prop.Property.Type);
        }

        // 3. Field on the owning type
        var (field, fieldDeclType) = FindFieldWithDecl(_owner, id.Name);
        if (field is not null)
        {
            if (fieldDeclType is not null &&
                !PropertyAccessHelper.CanAccess(_ctx.TypeTable, _owner, fieldDeclType, field.Access))
            {
                Error(id, $"Field '{id.Name}' is inaccessible due to its protection level.");
                return null;
            }
            if (field.IsStatic && AssetFacts.TryGetAssetName(field, out var fieldAssetName))
                id.Metadata[AssetReferenceNameMetadata] = fieldAssetName;
            return DefaultExpressionFacts.DescribeType(field.Type);
        }
        return null;
    }

    /// <summary>
    ///     Resolves a contextual-keyword identifier (<c>value</c> / <c>field</c>) that may
    ///     also be used as a local variable name.
    /// </summary>
    private string? InferKeywordVar(string keyword)
    {
        if (Symbols.TryResolve(keyword, out var typeRef) && typeRef is not null)
            return DefaultExpressionFacts.DescribeType(typeRef);
        return null;
    }

    private string? InferUnary(TgmlUnaryExpr expr)
    {
        var t = InferType(expr.Operand);
        if (t is null) return null;

        ClearResolvedOperator(expr);
        if (TryResolveUnaryOperator(expr.Operator, expr.Operand, t, out var resolvedUnaryOwner, out var resolvedUnary))
        {
            ApplyImplicitConversion(DefaultExpressionFacts.DescribeType(resolvedUnary.Params[0].Type), expr.Operand);
            SetResolvedOperator(expr, resolvedUnaryOwner, resolvedUnary);
            return DefaultExpressionFacts.DescribeType(resolvedUnary.ReturnType);
        }

        switch (expr.Operator)
        {
            case "+":
            case "-":
                if (!TypeCompatibility.IsNumeric(t))
                    Error(expr, $"Operator '{expr.Operator}' cannot be applied to type '{t}'.");
                return TypeCompatibility.IsNumeric(t) ? t : null;

            case "~":
                if (!TypeCompatibility.IsNumeric(t))
                    Error(expr, $"Operator '~' requires 'number', got '{t}'.");
                return TypeCompatibility.IsNumeric(t) ? "number" : null;

            case "not":
                if (t != "bool")
                    Error(expr, $"Operator 'not' requires 'bool', got '{t}'.");
                return "bool";
        }

        return null;
    }

    private string? InferBinary(TgmlBinaryExpr expr)
    {
        var lt = InferType(expr.Left);
        var rt = InferType(expr.Right);

        if (lt is null || rt is null) return null;

        ClearResolvedOperator(expr);
        if (TryResolveBinaryOperator(expr.Operator, expr.Left, expr.Right, lt, rt, out var resolvedBinaryOwner, out var resolvedBinary))
        {
            ApplyImplicitConversion(DefaultExpressionFacts.DescribeType(resolvedBinary.Params[0].Type), expr.Left);
            ApplyImplicitConversion(DefaultExpressionFacts.DescribeType(resolvedBinary.Params[1].Type), expr.Right);
            SetResolvedOperator(expr, resolvedBinaryOwner, resolvedBinary);
            return DefaultExpressionFacts.DescribeType(resolvedBinary.ReturnType);
        }

        switch (expr.Operator)
        {
            case "+" or "-" or "*" or "/" or "%":
                if (expr.Operator == "+" && lt == "string" && rt == "string") return "string";
                if (!TypeCompatibility.IsNumeric(lt) || !TypeCompatibility.IsNumeric(rt))
                    Error(expr, $"Operator '{expr.Operator}' cannot be applied to '{lt}' and '{rt}'.");
                return TypeCompatibility.IsNumeric(lt) && TypeCompatibility.IsNumeric(rt) ? "number" : null;

            case "&" or "|" or "^" or "<<" or ">>":
                if (!TypeCompatibility.IsNumeric(lt) || !TypeCompatibility.IsNumeric(rt))
                    Error(expr,
                        $"Bitwise operator '{expr.Operator}' requires 'number' operands, got '{lt}' and '{rt}'.");
                return TypeCompatibility.IsNumeric(lt) && TypeCompatibility.IsNumeric(rt) ? "number" : null;

            case "and" or "or" or "&&" or "||":
                if (lt != "bool" || rt != "bool")
                    Error(expr,
                        $"Logical operator '{expr.Operator}' requires 'bool' operands, got '{lt}' and '{rt}'.");
                return "bool";

            case "<" or ">" or "<=" or ">=":
                if (!TypeCompatibility.IsNumeric(lt) || !TypeCompatibility.IsNumeric(rt))
                    Error(expr,
                        $"Relational operator '{expr.Operator}' requires numeric operands, got '{lt}' and '{rt}'.");
                return "bool";

            case "==" or "!=":
                if (lt != "null" && rt != "null" && !AreTypesComparable(lt, rt))
                    Error(expr, $"Operator '{expr.Operator}' cannot be applied to '{lt}' and '{rt}'.");
                return "bool";
        }

        return null;
    }

    private string? InferCast(TgmlCastExpr expr)
    {
        var targetType = DefaultExpressionFacts.DescribeType(expr.TargetType);
        DefaultExpressionFacts.TryApplyContextualType(expr.Operand, targetType);

        if (!CanConvertExpression(targetType, expr.Operand, allowExplicit: true, apply: true))
        {
            var sourceType = InferType(expr.Operand);
            if (sourceType is not null)
                Error(expr, $"Cannot convert type '{sourceType}' to '{targetType}'.");
        }

        return targetType;
    }

    private string? InferTernary(TgmlTernaryExpr expr)
    {
        CheckExpr(expr.Condition);
        var tt = InferType(expr.ThenExpr);
        var et = InferType(expr.ElseExpr);
        return tt == et ? tt : null;
    }

    private string? InferDefault(TgmlDefaultExpr expr)
    {
        return DefaultExpressionFacts.GetEffectiveTypeName(expr);
    }

}
