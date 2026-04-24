namespace TypedGML.Transpiler.Generation;

internal sealed record GameObjectConstructionPlan(
    string ConstructorName,
    IReadOnlyList<string> Args);
