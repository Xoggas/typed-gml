using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking;

public sealed record BoundCallArguments(
    IReadOnlyList<TgmlParam> Parameters,
    IReadOnlyList<TgmlExpression> Arguments,
    int ExactMatchCount,
    int DefaultsUsed);
