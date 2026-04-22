using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking;

public sealed partial class ExprChecker
{
    private bool TryResolveMethodGroup(
        DelegateSignature targetSignature,
        TgmlExpression expr,
        bool apply,
        out TgmlMethodDecl? resolvedMethod)
    {
        expr.Metadata.Remove(DelegateMethodGroupMetadata);
        resolvedMethod = null;

        var candidates = expr switch
        {
            TgmlIdExpr id => ResolveBareMethodGroupCandidates(id),
            TgmlFieldAccessExpr fieldAccess => ResolveMemberMethodGroupCandidates(fieldAccess),
            _ => []
        };

        if (candidates.Count == 0)
            return false;

        var compatible = candidates
            .Where(method => MethodMatchesDelegateSignature(method, targetSignature))
            .ToList();

        if (compatible.Count == 0)
        {
            if (apply)
                Error(expr, $"Method group '{DescribeMethodGroup(expr)}' does not match delegate type '{targetSignature.TypeName}'.");
            return false;
        }

        var ranked = compatible
            .Select(method => (Method: method, Score: ScoreMethodGroupCandidate(method, targetSignature)))
            .OrderByDescending(x => x.Score)
            .ToList();

        if (ranked.Count > 1 && ranked[0].Score == ranked[1].Score)
        {
            if (apply)
                Error(expr, $"Method group '{DescribeMethodGroup(expr)}' is ambiguous for delegate type '{targetSignature.TypeName}'.");
            return false;
        }

        resolvedMethod = ranked[0].Method;
        if (apply)
            expr.Metadata[DelegateMethodGroupMetadata] = true;
        return true;
    }

    private List<TgmlMethodDecl> ResolveBareMethodGroupCandidates(TgmlIdExpr id)
    {
        if (Symbols.TryResolve(id.Name, out _))
            return [];

        if (ResolvePropertyOnCurrentType(id.Name) is not null)
            return [];

        if (FindFieldInHierarchy(_owner, id.Name) is not null)
            return [];

        return FindMethodsInHierarchy(_owner, id.Name)
            .Where(IsMethodGroupCandidate)
            .ToList();
    }

    private List<TgmlMethodDecl> ResolveMemberMethodGroupCandidates(TgmlFieldAccessExpr fieldAccess)
    {
        var targetType = InferType(fieldAccess.Target);
        if (targetType is null ||
            !_ctx.TypeTable.TryResolve(targetType, out var targetDecl) ||
            targetDecl is null)
        {
            return [];
        }

        return FindMethodsInHierarchy(targetDecl, fieldAccess.FieldName)
            .Where(IsMethodGroupCandidate)
            .ToList();
    }

    private static bool IsMethodGroupCandidate(TgmlMethodDecl method)
    {
        return !method.IsAbstract &&
               !method.IsUserDefinedOperator &&
               method.TypeParams.Count == 0;
    }

    private bool MethodMatchesDelegateSignature(TgmlMethodDecl method, DelegateSignature signature)
    {
        if (method.Params.Count != signature.Parameters.Count)
            return false;

        for (var i = 0; i < method.Params.Count; i++)
        {
            var methodParamType = DefaultExpressionFacts.DescribeType(method.Params[i].Type);
            var delegateParamType = signature.Parameters[i].TypeName;
            if (!CanAssignTypeToType(methodParamType, delegateParamType))
                return false;
        }

        var methodReturnType = DefaultExpressionFacts.DescribeType(method.ReturnType);
        return CanAssignTypeToType(signature.ReturnType, methodReturnType);
    }

    private static int ScoreMethodGroupCandidate(TgmlMethodDecl method, DelegateSignature signature)
    {
        var score = 0;
        for (var i = 0; i < Math.Min(method.Params.Count, signature.Parameters.Count); i++)
        {
            var methodParamType = DefaultExpressionFacts.DescribeType(method.Params[i].Type);
            if (methodParamType == signature.Parameters[i].TypeName)
                score += 2;
            else
                score += 1;
        }

        if (DefaultExpressionFacts.DescribeType(method.ReturnType) == signature.ReturnType)
            score += 2;

        return score;
    }

    private static string DescribeMethodGroup(TgmlExpression expr)
    {
        return expr switch
        {
            TgmlIdExpr id => id.Name,
            TgmlFieldAccessExpr fieldAccess => fieldAccess.FieldName,
            _ => "expression"
        };
    }

}
