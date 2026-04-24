using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Generation;

internal static class GameObjectInitPlanner
{
    public static IReadOnlyList<GameObjectInitPlan> BuildAll(string baseScriptName, TgmlClassDecl cls)
    {
        if (cls.Constructors.Count == 0)
            return [new GameObjectInitPlan(null, $"{baseScriptName}_0", [])];

        return cls.Constructors
            .Select((ctor, index) => Build(baseScriptName, ctor, index))
            .ToList();
    }

    public static GameObjectInitPlan Build(string baseScriptName, TgmlClassDecl cls, TgmlConstructorDecl ctor)
    {
        var ctorIndex = cls.Constructors.IndexOf(ctor);
        return Build(baseScriptName, ctor, Math.Max(ctorIndex, 0));
    }

    private static GameObjectInitPlan Build(string baseScriptName, TgmlConstructorDecl ctor, int index)
        => new(ctor, $"{baseScriptName}_{index}", ctor.Params);
}
