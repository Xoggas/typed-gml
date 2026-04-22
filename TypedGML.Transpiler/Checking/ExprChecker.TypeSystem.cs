using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking;

public sealed partial class ExprChecker
{
    private bool CanAssignTypeToType(string targetType, string sourceType)
    {
        if (TypeCompatibility.AreAssignable(targetType, sourceType))
            return true;

        if (DelegateFacts.IsDelegateType(_ctx.TypeTable, targetType) || DelegateFacts.IsDelegateType(_ctx.TypeTable, sourceType))
            return targetType == sourceType;

        if (targetType == "any")
            return true;

        if (targetType == "object")
            return sourceType != "void";

        // System.Object is the universal base — any class, struct, or interface is assignable to it.
        if (targetType is "System.Object" or "Object" &&
            TryResolveTypeDecl(sourceType, out var srcDecl2) &&
            srcDecl2 is TgmlClassDecl or TgmlStructDecl or TgmlInterfaceDecl)
            return true;

        if (targetType == "struct")
            return TryResolveTypeDecl(sourceType, out var structDecl) && structDecl is TgmlStructDecl;

        // Resolve both sides to qualified names and re-check primitive equivalence.
        // Handles e.g. param type "Bool" (unqualified, inside System namespace) vs primitive "bool".
        var resolvedTarget = TryResolveTypeDecl(targetType, out var tDecl) ? (tDecl?.QualifiedName ?? targetType) : targetType;
        var resolvedSource = TryResolveTypeDecl(sourceType, out var sDecl) ? (sDecl?.QualifiedName ?? sourceType) : sourceType;
        if (TypeCompatibility.ArePrimitiveEquivalent(resolvedTarget, resolvedSource))
            return true;

        // T[] is implicitly assignable to ICollection<T> and IList<T>
        if (sourceType.EndsWith("[]", StringComparison.Ordinal))
        {
            var elementType = sourceType[..^2];
            var baseTarget = targetType;
            var ltIdx = baseTarget.IndexOf('<');
            if (ltIdx >= 0) baseTarget = baseTarget[..ltIdx];
            if (baseTarget is "ICollection" or "System.Collections.ICollection"
                          or "IList" or "System.Collections.IList")
                return true;
        }

        return IsReferenceAssignable(targetType, sourceType);
    }

    private bool CanExplicitlyCastTypeToType(string targetType, string sourceType)
    {
        if (CanAssignTypeToType(targetType, sourceType) || CanAssignTypeToType(sourceType, targetType))
            return true;

        if (BuiltinTypeFacts.IsPrimitive(targetType) || BuiltinTypeFacts.IsPrimitive(sourceType))
            return false;

        if (sourceType == "null")
            return true;

        if (!TryResolveTypeDecl(targetType, out var targetDecl) || !TryResolveTypeDecl(sourceType, out var sourceDecl))
            return false;

        if (sourceDecl is TgmlClassDecl && targetDecl is TgmlStructDecl)
            return false;

        if (sourceDecl is TgmlStructDecl && targetDecl is TgmlClassDecl)
            return false;

        return sourceDecl is TgmlClassDecl or TgmlInterfaceDecl &&
               targetDecl is TgmlClassDecl or TgmlInterfaceDecl;
    }

    private bool AreTypesComparable(string leftType, string rightType)
    {
        return TypeCompatibility.AreComparable(leftType, rightType) ||
               CanAssignTypeToType(leftType, rightType) ||
               CanAssignTypeToType(rightType, leftType);
    }

    private bool IsReferenceAssignable(string targetType, string sourceType)
    {
        if (!TryResolveTypeDecl(targetType, out var targetDecl) ||
            !TryResolveTypeDecl(sourceType, out var sourceDecl) ||
            targetDecl is null ||
            sourceDecl is null)
            return false;

        if ((targetDecl.QualifiedName is not null && targetDecl.QualifiedName == sourceDecl.QualifiedName) ||
            targetDecl.Name == sourceDecl.Name)
            return true;

        var reachable = CollectReachableTypeNames(sourceDecl, new HashSet<string>(StringComparer.Ordinal));
        return reachable.Contains(targetType) ||
               (targetDecl.QualifiedName is not null && reachable.Contains(targetDecl.QualifiedName)) ||
               reachable.Contains(targetDecl.Name);
    }

    private bool TryResolveUserDefinedConversion(
        string sourceType,
        string targetType,
        bool allowExplicit,
        out TgmlTypeDecl owner,
        out TgmlMethodDecl method)
    {
        var candidates = new List<(TgmlTypeDecl Owner, TgmlMethodDecl Method)>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        CollectConversionCandidates(sourceType, sourceType, targetType, allowExplicit, candidates, seen);

        // Also search the BCL primitive wrapper (e.g. bool → System.Bool) for conversions
        var mappedSource = MapPrimitiveType(sourceType);
        if (mappedSource is not null && mappedSource != sourceType)
            CollectConversionCandidates(mappedSource, sourceType, targetType, allowExplicit, candidates, seen);

        // Also search System.Object for implicit conversions that apply to all types
        if (_ctx.TypeTable.TryResolve("System.Object", out var objDecl) && objDecl is not null)
            CollectConversionCandidates("System.Object", sourceType, targetType, allowExplicit, candidates, seen);

        if (targetType != sourceType)
            CollectConversionCandidates(targetType, sourceType, targetType, allowExplicit, candidates, seen);

        if (candidates.Count == 0)
        {
            owner = null!;
            method = null!;
            return false;
        }

        var ranked = candidates
            .Select(candidate => (candidate.Owner, candidate.Method, Score: ScoreConversionCandidate(candidate.Method, sourceType, targetType)))
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

    private void CollectConversionCandidates(
        string lookupType,
        string sourceType,
        string targetType,
        bool allowExplicit,
        List<(TgmlTypeDecl Owner, TgmlMethodDecl Method)> results,
        HashSet<string> seen)
    {
        if (!TryResolveTypeDecl(lookupType, out var ownerDecl))
            return;

        foreach (var candidate in CollectMethodsInHierarchy(ownerDecl, method => method.IsConversionOperator))
        {
            if ((!allowExplicit && candidate.Method.Conversion != ConversionModifier.Implicit) ||
                candidate.Method.Params.Count != 1)
            {
                continue;
            }

            var paramType = DefaultExpressionFacts.DescribeType(candidate.Method.Params[0].Type);
            var returnType = DefaultExpressionFacts.DescribeType(candidate.Method.ReturnType);
            if (!CanAssignTypeToType(paramType, sourceType) ||
                !CanAssignTypeToType(targetType, returnType))
            {
                continue;
            }

            var key = $"{candidate.Owner.QualifiedName ?? candidate.Owner.Name}:{OperatorFacts.GetHelperName(candidate.Owner, candidate.Method)}";
            if (seen.Add(key))
                results.Add(candidate);
        }
    }

    private int ScoreConversionCandidate(TgmlMethodDecl method, string sourceType, string targetType)
    {
        var score = 0;
        var paramType = DefaultExpressionFacts.DescribeType(method.Params[0].Type);
        var returnType = DefaultExpressionFacts.DescribeType(method.ReturnType);

        if (paramType == sourceType)
            score += 2;

        if (returnType == targetType)
            score += 2;

        if (method.Conversion == ConversionModifier.Implicit)
            score += 1;

        return score;
    }

}
