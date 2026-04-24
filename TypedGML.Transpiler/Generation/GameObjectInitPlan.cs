using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Generation;

internal sealed record GameObjectInitPlan(
    TgmlConstructorDecl? Constructor,
    string ScriptName,
    IReadOnlyList<TgmlParam> Parameters);
