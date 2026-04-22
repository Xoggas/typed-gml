using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking;

public sealed partial class ExprChecker
{
    private bool TryResolveUnaryOperator(
        string operatorToken,
        TgmlExpression operand,
        string operandType,
        out TgmlTypeDecl owner,
        out TgmlMethodDecl method)
    {
        if (!TryResolveTypeDecl(operandType, out var operandDecl))
        {
            owner = null!;
            method = null!;
            return false;
        }

        var candidates = CollectMethodsInHierarchy(
                operandDecl,
                candidate => candidate.IsOperatorOverload &&
                             candidate.OperatorToken == operatorToken &&
                             candidate.Params.Count == 1)
            .Where(candidate => CanAssignTypeToType(DefaultExpressionFacts.DescribeType(candidate.Method.Params[0].Type), operandType))
            .ToList();

        return TryChooseOperatorCandidate(candidates, [operand], out owner, out method);
    }

    private bool TryResolveBinaryOperator(
        string operatorToken,
        TgmlExpression left,
        TgmlExpression right,
        string leftType,
        string rightType,
        out TgmlTypeDecl owner,
        out TgmlMethodDecl method)
    {
        var candidates = new List<(TgmlTypeDecl Owner, TgmlMethodDecl Method)>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        CollectOperatorCandidates(leftType, operatorToken, seen, candidates);
        if (rightType != leftType)
            CollectOperatorCandidates(rightType, operatorToken, seen, candidates);

        candidates = candidates
            .Where(candidate =>
                candidate.Method.Params.Count == 2 &&
                CanAssignTypeToType(DefaultExpressionFacts.DescribeType(candidate.Method.Params[0].Type), leftType) &&
                CanAssignTypeToType(DefaultExpressionFacts.DescribeType(candidate.Method.Params[1].Type), rightType))
            .ToList();

        return TryChooseOperatorCandidate(candidates, [left, right], out owner, out method);
    }

    private void CollectOperatorCandidates(
        string lookupType,
        string operatorToken,
        HashSet<string> seen,
        List<(TgmlTypeDecl Owner, TgmlMethodDecl Method)> results)
    {
        if (!TryResolveTypeDecl(lookupType, out var ownerDecl))
            return;

        foreach (var candidate in CollectMethodsInHierarchy(
                     ownerDecl,
                     method => method.IsOperatorOverload && method.OperatorToken == operatorToken))
        {
            var key = $"{candidate.Owner.QualifiedName ?? candidate.Owner.Name}:{OperatorFacts.GetHelperName(candidate.Owner, candidate.Method)}";
            if (seen.Add(key))
                results.Add(candidate);
        }
    }

    private bool TryChooseOperatorCandidate(
        List<(TgmlTypeDecl Owner, TgmlMethodDecl Method)> candidates,
        IReadOnlyList<TgmlExpression> operands,
        out TgmlTypeDecl owner,
        out TgmlMethodDecl method)
    {
        if (candidates.Count == 0)
        {
            owner = null!;
            method = null!;
            return false;
        }

        var ranked = candidates
            .Select(candidate => (candidate.Owner, candidate.Method, Score: ScoreOperatorCandidate(candidate.Method, operands)))
            .OrderByDescending(x => x.Score)
            .ToList();

        if (ranked.Count > 1 && ranked[0].Score == ranked[1].Score)
        {
            owner = null!;
            method = null!;
            return false;
        }

        owner = ranked[0].Owner;
        method = ranked[0].Method;
        return true;
    }

    private int ScoreOperatorCandidate(TgmlMethodDecl method, IReadOnlyList<TgmlExpression> operands)
    {
        var score = 0;
        for (var i = 0; i < Math.Min(method.Params.Count, operands.Count); i++)
        {
            var operandType = InferType(operands[i]);
            var paramType = DefaultExpressionFacts.DescribeType(method.Params[i].Type);
            if (operandType == paramType)
                score += 2;
            else if (operandType is not null && CanAssignTypeToType(paramType, operandType))
                score += 1;
        }

        return score;
    }

    private List<(TgmlTypeDecl Owner, TgmlMethodDecl Method)> CollectMethodsInHierarchy(
        TgmlTypeDecl? type,
        Func<TgmlMethodDecl, bool> predicate)
    {
        var results = new List<(TgmlTypeDecl Owner, TgmlMethodDecl Method)>();
        CollectMethodsInHierarchy(type, predicate, new HashSet<string>(StringComparer.Ordinal), results);
        return results;
    }

    private void CollectMethodsInHierarchy(
        TgmlTypeDecl? type,
        Func<TgmlMethodDecl, bool> predicate,
        HashSet<string> visited,
        List<(TgmlTypeDecl Owner, TgmlMethodDecl Method)> results)
    {
        if (type is null)
            return;

        var key = type.QualifiedName ?? type.Name;
        if (!visited.Add(key)) return;

        switch (type)
        {
            case TgmlClassDecl cls:
                results.AddRange(cls.Methods.Where(predicate).Select(method => ((TgmlTypeDecl)cls, method)));
                foreach (var baseRef in cls.BaseTypes)
                {
                    if (_ctx.TypeTable.TryResolve(baseRef.Name.Full, out var baseDecl) && baseDecl is not null)
                        CollectMethodsInHierarchy(baseDecl, predicate, visited, results);
                }
                break;

            case TgmlStructDecl str:
                results.AddRange(str.Methods.Where(predicate).Select(method => ((TgmlTypeDecl)str, method)));
                foreach (var baseRef in str.BaseTypes)
                {
                    if (_ctx.TypeTable.TryResolve(baseRef.Name.Full, out var baseDecl) && baseDecl is not null)
                        CollectMethodsInHierarchy(baseDecl, predicate, visited, results);
                }
                break;
        }
    }

}
