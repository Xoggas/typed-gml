using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking;

public sealed record BoundCallArguments(
    IReadOnlyList<TgmlParam> Parameters,
    IReadOnlyList<TgmlExpression> Arguments,
    int ExactMatchCount,
    int DefaultsUsed);

public static class CallArgumentBinder
{
    public static bool TryBind(
        IReadOnlyList<TgmlParam> parameters,
        IReadOnlyList<TgmlArgument> suppliedArgs,
        Func<TgmlExpression, string?>? inferType,
        Func<string, TgmlExpression, bool>? canAssign,
        out BoundCallArguments? bound,
        out string? error)
    {
        var boundArgs = new TgmlExpression?[parameters.Count];
        var nextPositionalIndex = 0;

        foreach (var arg in suppliedArgs)
        {
            int targetIndex;
            if (arg.Name is null)
            {
                targetIndex = nextPositionalIndex;
                while (targetIndex < boundArgs.Length && boundArgs[targetIndex] is not null)
                    targetIndex++;

                if (targetIndex >= boundArgs.Length)
                {
                    bound = null;
                    error = "Too many arguments.";
                    return false;
                }

                nextPositionalIndex = targetIndex + 1;
            }
            else
            {
                targetIndex = -1;
                for (var i = 0; i < parameters.Count; i++)
                {
                    if (!string.Equals(parameters[i].Name, arg.Name, StringComparison.Ordinal))
                        continue;

                    targetIndex = i;
                    break;
                }

                if (targetIndex < 0)
                {
                    bound = null;
                    error = $"Unknown parameter '{arg.Name}'.";
                    return false;
                }
            }

            if (boundArgs[targetIndex] is not null)
            {
                bound = null;
                error = $"Parameter '{parameters[targetIndex].Name}' is specified more than once.";
                return false;
            }

            DefaultExpressionFacts.TryApplyContextualType(arg.Value, DefaultExpressionFacts.DescribeType(parameters[targetIndex].Type));
            var inferredType = inferType?.Invoke(arg.Value);
            if (inferredType is not null &&
                !(canAssign?.Invoke(DefaultExpressionFacts.DescribeType(parameters[targetIndex].Type), arg.Value) ??
                  TypeCompatibility.AreAssignable(DefaultExpressionFacts.DescribeType(parameters[targetIndex].Type), inferredType)))
            {
                bound = null;
                error = $"Cannot assign argument of type '{inferredType}' to parameter '{parameters[targetIndex].Name}' of type '{DefaultExpressionFacts.DescribeType(parameters[targetIndex].Type)}'.";
                return false;
            }

            boundArgs[targetIndex] = arg.Value;
        }

        var exactMatches = 0;
        var defaultsUsed = 0;
        var orderedArgs = new List<TgmlExpression>(parameters.Count);

        for (var i = 0; i < parameters.Count; i++)
        {
            var argExpr = boundArgs[i];
            if (argExpr is null)
            {
                if (parameters[i].Default is null)
                {
                    bound = null;
                    error = $"No argument was given for required parameter '{parameters[i].Name}'.";
                    return false;
                }

                argExpr = parameters[i].Default;
                DefaultExpressionFacts.TryApplyContextualType(argExpr!, DefaultExpressionFacts.DescribeType(parameters[i].Type));
                defaultsUsed++;
            }
            else
            {
                var inferredType = inferType?.Invoke(argExpr);
                if (inferredType == DefaultExpressionFacts.DescribeType(parameters[i].Type))
                    exactMatches++;
            }

            orderedArgs.Add(argExpr!);
        }

        bound = new BoundCallArguments(parameters, orderedArgs, exactMatches, defaultsUsed);
        error = null;
        return true;
    }

    public static bool TryResolveOverload<TCandidate>(
        IReadOnlyList<TCandidate> candidates,
        Func<TCandidate, IReadOnlyList<TgmlParam>> parameterSelector,
        IReadOnlyList<TgmlArgument> suppliedArgs,
        Func<TgmlExpression, string?>? inferType,
        Func<string, TgmlExpression, bool>? canAssign,
        out TCandidate? resolvedCandidate,
        out BoundCallArguments? bound,
        out string? error)
    {
        resolvedCandidate = default;
        bound = null;
        error = null;

        var successes = new List<(TCandidate Candidate, BoundCallArguments Bound)>();
        string? firstError = null;

        foreach (var candidate in candidates)
        {
            if (TryBind(parameterSelector(candidate), suppliedArgs, inferType, canAssign, out var candidateBound, out var candidateError))
            {
                successes.Add((candidate, candidateBound!));
            }
            else if (firstError is null)
            {
                firstError = candidateError;
            }
        }

        if (successes.Count == 0)
        {
            error = firstError ?? "No overload matches the supplied arguments.";
            return false;
        }

        if (successes.Count == 1)
        {
            resolvedCandidate = successes[0].Candidate;
            bound = successes[0].Bound;
            return true;
        }

        var ordered = successes
            .OrderByDescending(x => x.Bound.ExactMatchCount)
            .ThenBy(x => x.Bound.DefaultsUsed)
            .ToList();

        var best = ordered[0];
        var second = ordered[1];
        if (best.Bound.ExactMatchCount == second.Bound.ExactMatchCount &&
            best.Bound.DefaultsUsed == second.Bound.DefaultsUsed)
        {
            error = "The call is ambiguous between multiple overloads.";
            return false;
        }

        resolvedCandidate = best.Candidate;
        bound = best.Bound;
        return true;
    }

    public static bool TryBind(
        IReadOnlyList<TgmlParam> parameters,
        IReadOnlyList<TgmlArgument> suppliedArgs,
        Func<TgmlExpression, string?>? inferType,
        out BoundCallArguments? bound,
        out string? error)
    {
        return TryBind(parameters, suppliedArgs, inferType, null, out bound, out error);
    }

    public static bool TryResolveOverload<TCandidate>(
        IReadOnlyList<TCandidate> candidates,
        Func<TCandidate, IReadOnlyList<TgmlParam>> parameterSelector,
        IReadOnlyList<TgmlArgument> suppliedArgs,
        Func<TgmlExpression, string?>? inferType,
        out TCandidate? resolvedCandidate,
        out BoundCallArguments? bound,
        out string? error)
    {
        return TryResolveOverload(
            candidates,
            parameterSelector,
            suppliedArgs,
            inferType,
            null,
            out resolvedCandidate,
            out bound,
            out error);
    }
}
