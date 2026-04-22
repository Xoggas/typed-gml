using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking;

public sealed partial class ExprChecker
{
    private bool ValidateLambdaAgainstDelegate(TgmlLambdaExpr lambda, DelegateSignature signature, bool apply)
    {
        if (lambda.Params.Count != signature.Parameters.Count)
        {
            if (apply)
            {
                Error(lambda, $"Lambda has {lambda.Params.Count} parameter(s) but delegate '{signature.TypeName}' expects {signature.Parameters.Count}.");
                return true; // suppress generic "cannot convert" error from caller
            }
            return false;
        }

        var paramBindings = new List<TgmlParam>(signature.Parameters.Count);
        for (var i = 0; i < signature.Parameters.Count; i++)
        {
            var lambdaParam = lambda.Params[i];
            var delegateParam = signature.Parameters[i];
            var lambdaParamType = DefaultExpressionFacts.DescribeType(lambdaParam.Type);
            if (lambdaParamType != "?" && lambdaParamType != delegateParam.TypeName)
            {
                if (apply)
                {
                    Error(lambda, $"Lambda parameter '{lambdaParam.Name}' has type '{lambdaParamType}' but delegate '{signature.TypeName}' expects '{delegateParam.TypeName}'.");
                    return true; // suppress generic error
                }
                return false;
            }

            paramBindings.Add(new TgmlParam
            {
                Name = lambdaParam.Name,
                Type = MakeTypeRef(delegateParam.TypeName),
                Decorators = lambdaParam.Decorators
            });
        }

        if (!apply)
            return true;

        if (apply)
            lambda.Metadata[ContextualDelegateTypeMetadata] = signature.TypeName;

        var inner = new ExprChecker(_ctx, _file, Symbols, _owner, signature.ReturnType);
        inner.Symbols.PushScope();
        foreach (var binding in paramBindings)
            inner.Symbols.Define(binding.Name, binding.Type);

        if (lambda.ExprBody is not null)
        {
            if (signature.ReturnType == "void")
            {
                inner.Error(lambda, "Expression-bodied lambda cannot be converted to a void delegate.");
                inner.Symbols.PopScope();
                return true;
            }

            inner.CheckAssignCompatibility(signature.ReturnType, lambda.ExprBody, lambda.Line, lambda.Column,
                $"Lambda return type mismatch: cannot convert '{{1}}' to '{signature.ReturnType}'.");
        }
        else if (lambda.BlockBody is not null)
        {
            inner.CheckBlock(lambda.BlockBody);

            if (signature.ReturnType != "void" && !BlockAlwaysReturns(lambda.BlockBody))
                Error(lambda, $"Lambda assigned to '{signature.TypeName}' must return a value of type '{signature.ReturnType}' on all code paths.");
        }

        inner.Symbols.PopScope();
        return true;
    }

    private bool CanAssignImplicitly(string targetType, TgmlExpression expr)
        => CanConvertExpression(targetType, expr, allowExplicit: false, apply: false);

    private void ApplyBoundArgumentConversions(
        IReadOnlyList<TgmlParam> parameters,
        IReadOnlyList<TgmlExpression> arguments)
    {
        for (var i = 0; i < Math.Min(parameters.Count, arguments.Count); i++)
            ApplyImplicitConversion(DefaultExpressionFacts.DescribeType(parameters[i].Type), arguments[i]);
    }

    private bool ApplyImplicitConversion(string targetType, TgmlExpression expr)
        => CanConvertExpression(targetType, expr, allowExplicit: false, apply: true);

    private bool CanConvertExpression(string targetType, TgmlExpression expr, bool allowExplicit, bool apply)
    {
        DefaultExpressionFacts.TryApplyContextualType(expr, targetType);

        if (expr is TgmlLambdaExpr lambda && DelegateFacts.TryResolveSignature(_ctx.TypeTable, targetType, out var delegateSignature))
            return ValidateLambdaAgainstDelegate(lambda, delegateSignature, apply);

        if (DelegateFacts.TryResolveSignature(_ctx.TypeTable, targetType, out var targetDelegateSignature) &&
            TryResolveMethodGroup(targetDelegateSignature, expr, apply, out _))
        {
            if (apply)
                expr.Metadata[ContextualDelegateTypeMetadata] = targetType;
            return true;
        }

        var sourceType = InferType(expr);
        if (sourceType is null)
        {
            if (apply)
                ClearResolvedConversion(expr);
            return true;
        }

        if (CanAssignTypeToType(targetType, sourceType))
        {
            if (apply)
                ClearResolvedConversion(expr);
            return true;
        }

        if (TryResolveUserDefinedConversion(sourceType, targetType, allowExplicit, out var conversionOwner, out var conversionMethod))
        {
            if (apply)
                SetResolvedConversion(expr, conversionOwner, conversionMethod);
            return true;
        }

        if (allowExplicit && CanExplicitlyCastTypeToType(targetType, sourceType))
        {
            if (apply)
                ClearResolvedConversion(expr);
            return true;
        }

        return false;
    }

}
