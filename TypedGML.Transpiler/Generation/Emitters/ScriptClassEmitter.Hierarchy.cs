using TypedGML.Transpiler.Checking;
using TypedGML.Transpiler.Generation.Emitters.Atomic;
using TypedGML.Transpiler.Generation.Helpers;
using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Generation.Emitters;

public sealed partial class ScriptClassEmitter
{
    private static List<(TgmlClassDecl Decl, List<TgmlExpression>? BaseArgs)> CollectAncestorChain(
        TgmlTypeDecl decl,
        List<TgmlExpression>? ownBaseArgs,
        TypeTable typeTable)
    {
        var chain = new List<(TgmlClassDecl, List<TgmlExpression>?)>();
        var currentBases = GetBaseTypeList(decl);
        var currentBaseArgs = ownBaseArgs;

        while (currentBases is not null)
        {
            TgmlClassDecl? parent = null;
            foreach (var bt in currentBases)
            {
                typeTable.TryResolve(bt.Name.Full, out var btDecl);
                if (btDecl is TgmlClassDecl pc)
                {
                    parent = pc;
                    break;
                }
            }

            if (parent is null)
                break;

            chain.Add((parent, currentBaseArgs));
            currentBaseArgs = GetNormalizedBaseArgs(FindMatchingConstructor(parent, currentBaseArgs));
            currentBases = parent.BaseTypes;
        }

        return chain;
    }

    private static void EmitAncestorContribution(
        TgmlClassDecl ancestor,
        List<TgmlExpression>? baseArgs,
        GenerationContext ctx,
        ExpressionEmitter exprEmit,
        StatementEmitter stmtEmit,
        GmlWriter w)
    {
        var aGmlName = ancestor.QualifiedName?.Replace(".", "_") ?? ancestor.Name;
        FieldInitEmitter.Emit(ancestor.Fields, aGmlName, exprEmit, w);
        BackingFieldEmitter.Emit(ancestor.Properties, exprEmit, ctx, w, useTypeDefault: true);

        var matchedCtor = FindMatchingConstructor(ancestor, baseArgs);
        if (matchedCtor?.Body is { } body)
            stmtEmit.EmitBlock(body, w);

        StaticMethodsEmitter.Emit(ancestor.Methods.Where(m => !IsCompilerSynthesized(m)).ToList(), ctx, w);
        foreach (var prop in ancestor.Properties.Where(p => !AssetFacts.TryGetAssetName(p, out _)))
        {
            w.WriteLine();
            PropertyAccessorEmitter.EmitProperty(prop, ctx, w, isStatic: true);
        }
    }

    private static List<TgmlTypeRef>? GetBaseTypeList(TgmlTypeDecl decl) => decl switch
    {
        TgmlClassDecl c => c.BaseTypes,
        TgmlStructDecl s => s.BaseTypes,
        _ => null
    };

    private static List<TgmlExpression>? GetNormalizedBaseArgs(TgmlConstructorDecl? ctor)
    {
        if (ctor is null)
            return null;
        if (ctor.Metadata.TryGetValue("NormalizedBaseArgs", out var value) && value is List<TgmlExpression> normalized)
            return normalized;
        return ctor.BaseArgs?.Select(a => a.Value).ToList();
    }

    private static TgmlConstructorDecl? FindMatchingConstructor(TgmlClassDecl ancestor, List<TgmlExpression>? baseArgs)
    {
        if (ancestor.Constructors.Count == 0)
            return null;
        if (baseArgs is null)
            return ancestor.Constructors[0];

        var byCount = ancestor.Constructors.Where(c => c.Params.Count == baseArgs.Count).ToList();
        return byCount.Count == 1 ? byCount[0] : ancestor.Constructors[0];
    }
}
