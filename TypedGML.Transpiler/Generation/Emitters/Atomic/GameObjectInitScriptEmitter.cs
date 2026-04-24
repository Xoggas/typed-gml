using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Generation.Emitters.Atomic;

internal static class GameObjectInitScriptEmitter
{
    public static GeneratedFile? Emit(
        TgmlClassDecl cls,
        string initScriptName,
        GenerationContext ctx)
    {
        var initPlans = GameObjectInitPlanner.BuildAll(initScriptName, cls);

        var writer = new GmlWriter();
        for (var i = 0; i < initPlans.Count; i++)
        {
            EmitOne(initPlans[i], cls, ctx, writer);
            if (i < initPlans.Count - 1)
                writer.WriteLine();
        }

        return new GeneratedFile($"Scripts/{initScriptName}/{initScriptName}.gml", writer.ToString());
    }

    private static void EmitOne(
        GameObjectInitPlan plan,
        TgmlClassDecl cls,
        GenerationContext ctx,
        GmlWriter writer)
    {
        var stmtEmit = new StatementEmitter(ctx);
        var exprEmit = new ExpressionEmitter(ctx);
        var paramStr = string.Join(", ", plan.Parameters.Select(p => p.Name));
        var createArgs = GameObjectConstructionHelper.BuildCreateArgs(
            plan.Constructor,
            plan.Parameters.Select(p => p.Name).ToList(),
            exprEmit);

        writer.WriteLine($"function {plan.ScriptName}({paramStr})");
        writer.OpenBrace();
        writer.WriteLine($"var inst = instance_create_layer({string.Join(", ", createArgs)}, {ctx.GmlObjectName(cls)});");
        if (plan.Constructor?.Body.Statements.Count > 0)
        {
            writer.WriteLine("with (inst)");
            writer.OpenBrace();
            stmtEmit.EmitBlock(plan.Constructor.Body, writer);
            writer.CloseBrace();
        }
        writer.WriteLine("return inst;");
        writer.CloseBrace();
    }
}
