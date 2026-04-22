using TypedGML.Transpiler.Checking;
using TypedGML.Transpiler.Generation.Emitters.Atomic;
using TypedGML.Transpiler.Generation.Helpers;
using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Generation.Emitters;

public sealed partial class ScriptClassEmitter
{
    private static void EmitSingleCtorBody(
        TgmlTypeDecl decl,
        string gmlName,
        string qualifiedTypeName,
        List<TgmlFieldDecl> fields,
        List<TgmlPropertyDecl> properties,
        List<TgmlMethodDecl> methods,
        List<TgmlConstructorDecl> constructors,
        string typeIds,
        List<string> typeArgParams,
        GenerationContext ctx,
        ExpressionEmitter exprEmit,
        StatementEmitter stmtEmit,
        GmlWriter w)
    {
        var ctor = constructors.FirstOrDefault();
        var ctorParams = ctor?.Params ?? [];
        var allParams = typeArgParams.Concat(ctorParams.Select(p => p.Name));

        w.WriteLine($"function {gmlName}({string.Join(", ", allParams)}) constructor");
        w.OpenBrace();
        TypeMetadataEmitter.EmitHeader(typeIds, typeArgParams, qualifiedTypeName, w);

        var ancestorChain = CollectAncestorChain(decl, GetNormalizedBaseArgs(ctor), ctx.TypeTable);
        foreach (var (ancestor, baseArgs) in ancestorChain)
            EmitBaseParameterBindings(ancestor, baseArgs, exprEmit, w);
        foreach (var (ancestor, baseArgs) in Enumerable.Reverse(ancestorChain))
            EmitAncestorContribution(ancestor, baseArgs, ctx, exprEmit, stmtEmit, w);

        FieldInitEmitter.Emit(fields, gmlName, exprEmit, w);
        BackingFieldEmitter.Emit(properties, exprEmit, ctx, w, useTypeDefault: true);

        if (ctor?.Body is { } ctorBody)
        {
            ctx.PushLocalScope(ctorParams.Select(p => p.Name));
            stmtEmit.EmitBlock(ctorBody, w);
            ctx.PopLocalScope();
        }

        EmitOwnMembers(properties, methods, ctx, gmlName, w);
        w.CloseBrace();
    }

    private static void EmitMultiCtorBody(
        TgmlTypeDecl decl,
        string gmlName,
        string qualifiedTypeName,
        List<TgmlFieldDecl> fields,
        List<TgmlPropertyDecl> properties,
        List<TgmlMethodDecl> methods,
        List<TgmlConstructorDecl> constructors,
        string typeIds,
        List<string> typeArgParams,
        int typeArgOffset,
        GenerationContext ctx,
        ExpressionEmitter exprEmit,
        StatementEmitter stmtEmit,
        GmlWriter w)
    {
        w.WriteLine($"function {gmlName}({string.Join(", ", typeArgParams)}) constructor");
        w.OpenBrace();
        TypeMetadataEmitter.EmitHeader(typeIds, typeArgParams, qualifiedTypeName, w);

        var sharedChain = CollectAncestorChain(decl, GetNormalizedBaseArgs(constructors[0]), ctx.TypeTable);
        foreach (var (ancestor, _) in Enumerable.Reverse(sharedChain))
            EmitSharedAncestorMembers(ancestor, ctx, exprEmit, w);

        FieldInitEmitter.Emit(fields, gmlName, exprEmit, w);
        BackingFieldEmitter.Emit(properties, exprEmit, ctx, w, useTypeDefault: true);

        var dispatchOrder = constructors
            .Select((c, idx) => (Ctor: c, Idx: idx))
            .OrderByDescending(x => OverloadDispatchHelper.CtorSpecificity(x.Ctor))
            .ToList();

        for (var di = 0; di < dispatchOrder.Count; di++)
        {
            var (overload, _) = dispatchOrder[di];
            var isLast = di == dispatchOrder.Count - 1;
            var keyword = di == 0 ? "if" : isLast ? "else" : "else if";
            var condition = isLast ? string.Empty : $" ({OverloadDispatchHelper.BuildCtorDispatchCondition(overload, constructors, typeArgOffset)})";

            w.WriteLine($"{keyword}{condition}");
            w.OpenBrace();

            for (var pi = 0; pi < overload.Params.Count; pi++)
                w.WriteLine($"var {overload.Params[pi].Name} = argument[{pi + typeArgOffset}];");

            var overloadChain = CollectAncestorChain(decl, GetNormalizedBaseArgs(overload), ctx.TypeTable);
            foreach (var (ancestor, baseArgs) in overloadChain)
                EmitBaseParameterBindings(ancestor, baseArgs, exprEmit, w);
            foreach (var (ancestor, baseArgs) in Enumerable.Reverse(overloadChain))
            {
                var matchedCtor = FindMatchingConstructor(ancestor, baseArgs);
                if (matchedCtor?.Body is { } body)
                    stmtEmit.EmitBlock(body, w);
            }

            if (overload.Body is { } ctorBody)
            {
                ctx.PushLocalScope(overload.Params.Select(p => p.Name));
                stmtEmit.EmitBlock(ctorBody, w);
                ctx.PopLocalScope();
            }

            w.CloseBrace();
        }

        EmitOwnMembers(properties, methods, ctx, gmlName, w);
        w.CloseBrace();
    }

    private static void EmitBaseParameterBindings(
        TgmlClassDecl ancestor,
        List<TgmlExpression>? baseArgs,
        ExpressionEmitter exprEmit,
        GmlWriter w)
    {
        if (baseArgs is null)
            return;

        var matchedCtor = FindMatchingConstructor(ancestor, baseArgs);
        if (matchedCtor is null)
            return;

        for (var i = 0; i < Math.Min(matchedCtor.Params.Count, baseArgs.Count); i++)
        {
            var argStr = exprEmit.Emit(baseArgs[i]);
            if (argStr != matchedCtor.Params[i].Name)
                w.WriteLine($"var {matchedCtor.Params[i].Name} = {argStr};");
        }
    }

    private static void EmitSharedAncestorMembers(TgmlClassDecl ancestor, GenerationContext ctx, ExpressionEmitter exprEmit, GmlWriter w)
    {
        var aGmlName = ancestor.QualifiedName?.Replace(".", "_") ?? ancestor.Name;
        FieldInitEmitter.Emit(ancestor.Fields, aGmlName, exprEmit, w);
        BackingFieldEmitter.Emit(ancestor.Properties, exprEmit, ctx, w, useTypeDefault: true);
        StaticMethodsEmitter.Emit(ancestor.Methods.Where(m => !IsCompilerSynthesized(m)).ToList(), ctx, w);
        foreach (var prop in ancestor.Properties.Where(p => !AssetFacts.TryGetAssetName(p, out _)))
        {
            w.WriteLine();
            PropertyAccessorEmitter.EmitProperty(prop, ctx, w, isStatic: true);
        }
    }

    private static void EmitOwnMembers(
        IEnumerable<TgmlPropertyDecl> properties,
        IEnumerable<TgmlMethodDecl> methods,
        GenerationContext ctx,
        string gmlName,
        GmlWriter w)
    {
        StaticMethodsEmitter.Emit(methods.Where(m => !IsCompilerSynthesized(m)).ToList(), ctx, w);
        foreach (var prop in properties.Where(p => !AssetFacts.TryGetAssetName(p, out _)))
        {
            w.WriteLine();
            PropertyAccessorEmitter.EmitProperty(prop, ctx, w, isStatic: true);
        }

        TypeMetadataEmitter.EmitGetType(gmlName, w);
    }
}
