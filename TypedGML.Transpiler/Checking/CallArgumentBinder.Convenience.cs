using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking;

public static partial class CallArgumentBinder
{
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
