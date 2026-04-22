using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking;

public sealed partial class ExprChecker
{
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

}
