using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking.Checks;

public sealed partial class OperatorDeclarationCheck : IAtomicCheck
{
    public void Execute(TranspileContext context, IReadOnlyList<TgmlFile> files)
    {
        foreach (var file in files)
        foreach (var type in file.TypeDecls)
            CheckType(context, file, type);
    }

    private static void CheckType(TranspileContext ctx, TgmlFile file, TgmlTypeDecl decl)
    {
        switch (decl)
        {
            case TgmlClassDecl cls:
                CheckMethods(ctx, file, cls, cls.Methods.Where(m => m.IsUserDefinedOperator));
                foreach (var nested in cls.NestedTypes)
                    CheckType(ctx, file, nested);
                break;
            case TgmlStructDecl str:
                CheckMethods(ctx, file, str, str.Methods.Where(m => m.IsUserDefinedOperator));
                foreach (var nested in str.NestedTypes)
                    CheckType(ctx, file, nested);
                break;
        }
    }

    private static void CheckMethods(
        TranspileContext ctx,
        TgmlFile file,
        TgmlTypeDecl owner,
        IEnumerable<TgmlMethodDecl> methods)
    {
        foreach (var method in methods)
            CheckMethod(ctx, file, owner, method);
    }

    private static void CheckMethod(TranspileContext ctx, TgmlFile file, TgmlTypeDecl owner, TgmlMethodDecl method)
    {
        if (owner is TgmlClassDecl { IsGameObject: true })
        {
            ctx.AddError("User-defined operators are not supported on @Object classes.", file.FileName);
            return;
        }

        if (method.Access != AccessModifier.Public || !method.IsStatic)
            ctx.AddError($"User-defined operator '{method.Name}' must be declared 'public static'.", file.FileName);
        if (method.Modifiers.Virtual != VirtualModifier.None)
            ctx.AddError($"User-defined operator '{method.Name}' cannot be virtual, abstract, sealed, or override.", file.FileName);
        if (method.TypeParams.Count > 0)
            ctx.AddError($"User-defined operator '{method.Name}' cannot declare type parameters.", file.FileName);
        if (method.Params.Any(p => p.Default is not null))
            ctx.AddError($"User-defined operator '{method.Name}' cannot declare optional parameters.", file.FileName);
        if (method.Body is null && !NativeOperatorFacts.IsNativeOperator(method))
            ctx.AddError($"User-defined operator '{method.Name}' must declare a body.", file.FileName);
        if (method.ReturnType.Name.Full == "void")
            ctx.AddError($"User-defined operator '{method.Name}' cannot return 'void'.", file.FileName);

        if (method.IsConversionOperator)
            CheckConversionOperator(ctx, file, owner, method);
        else
            CheckOverloadedOperator(ctx, file, owner, method);
    }
}
