using TypedGML.Transpiler.Checking;
using TypedGML.Transpiler.Generation.Emitters.Atomic;
using TypedGML.Transpiler.Generation.Helpers;
using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Generation.Emitters;

public sealed partial class GameObjectEmitter
{
    private static void EmitCreateContribution(TgmlClassDecl sourceClass, GenerationContext ctx, GmlWriter w)
    {
        var stmtEmit = new StatementEmitter(ctx);
        var exprEmit = new ExpressionEmitter(ctx);
        var gmlName = sourceClass.QualifiedName?.Replace(".", "_") ?? sourceClass.Name;

        FieldInitEmitter.EmitForGameObject(sourceClass.Fields, gmlName, exprEmit, w);
        BackingFieldEmitter.Emit(sourceClass.Properties, exprEmit, ctx, w);

        var instanceMethods = sourceClass.Methods
            .Where(m => !IsNativeEvent(sourceClass, m, ctx) && !m.IsStatic)
            .ToList();
        InstanceMethodsEmitter.Emit(instanceMethods, sourceClass, ctx, w);

        foreach (var prop in sourceClass.Properties)
        {
            if (AssetFacts.TryGetAssetName(prop, out _))
                continue;

            w.WriteLine();
            PropertyAccessorEmitter.EmitProperty(prop, ctx, w, isStatic: false);
        }

        var defaultCtor = sourceClass.Constructors.FirstOrDefault(c => c.Params.Count == 0);
        if (defaultCtor?.Body is { } ctorBody)
            stmtEmit.EmitBlock(ctorBody, w);

        if (TryGetCreateEventMethod(sourceClass, ctx, out var createMethod))
        {
            ctx.CurrentMethodName = createMethod.Name;
            ctx.CurrentMethodIsOverride = createMethod.IsOverride;
            ctx.CurrentMethodOwnerType = sourceClass;
            ctx.CurrentNativeEventName = "Create";
            stmtEmit.EmitBlock(createMethod.Body!, w);
            ClearMethodContext(ctx);
        }
    }
}
