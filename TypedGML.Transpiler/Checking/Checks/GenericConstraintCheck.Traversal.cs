using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking.Checks;

public sealed partial class GenericConstraintCheck
{
    private static void CheckTypeDecl(TranspileContext ctx, TgmlFile file, TgmlTypeDecl decl)
    {
        switch (decl)
        {
            case TgmlClassDecl cls:
                CheckClassOrStruct(ctx, file, decl, cls.BaseTypes, cls.Fields, cls.Properties, cls.Methods, cls.Constructor, cls.NestedTypes);
                break;
            case TgmlStructDecl str:
                CheckClassOrStruct(ctx, file, decl, str.BaseTypes, str.Fields, str.Properties, str.Methods, str.Constructor, str.NestedTypes);
                break;
            case TgmlInterfaceDecl iface:
                foreach (var baseInterface in iface.BaseInterfaces)
                    CheckTypeRef(ctx, file, baseInterface, decl.TypeParams);

                foreach (var method in iface.Methods)
                {
                    var visible = decl.TypeParams.Concat(method.TypeParams).ToList();
                    CheckTypeRef(ctx, file, method.ReturnType, visible);
                    foreach (var param in method.Params)
                        CheckTypeRef(ctx, file, param.Type, visible);
                }

                foreach (var prop in iface.Properties)
                {
                    CheckTypeRef(ctx, file, prop.Type, decl.TypeParams);
                    if (prop.IndexParam is not null)
                        CheckTypeRef(ctx, file, prop.IndexParam.Type, decl.TypeParams);
                }
                break;
        }
    }

    private static void CheckClassOrStruct(
        TranspileContext ctx,
        TgmlFile file,
        TgmlTypeDecl decl,
        IEnumerable<TgmlTypeRef> baseTypes,
        IEnumerable<TgmlFieldDecl> fields,
        IEnumerable<TgmlPropertyDecl> properties,
        IEnumerable<TgmlMethodDecl> methods,
        TgmlConstructorDecl? ctor,
        IEnumerable<TgmlTypeDecl> nestedTypes)
    {
        foreach (var baseType in baseTypes)
            CheckTypeRef(ctx, file, baseType, decl.TypeParams);
        foreach (var field in fields)
            CheckTypeRef(ctx, file, field.Type, decl.TypeParams);
        foreach (var prop in properties)
            CheckTypeRef(ctx, file, prop.Type, decl.TypeParams);
        foreach (var method in methods)
            CheckMethod(ctx, file, method, decl.TypeParams);

        if (ctor is not null)
        {
            foreach (var param in ctor.Params)
                CheckTypeRef(ctx, file, param.Type, decl.TypeParams);
        }

        foreach (var nested in nestedTypes)
            CheckTypeDecl(ctx, file, nested);
    }

    private static void CheckTypeRef(
        TranspileContext ctx,
        TgmlFile file,
        TgmlTypeRef typeRef,
        IEnumerable<TgmlTypeParam> visibleTypeParams)
    {
        var visible = visibleTypeParams as List<TgmlTypeParam> ?? visibleTypeParams.ToList();

        foreach (var arg in typeRef.TypeArgs)
            CheckTypeRef(ctx, file, arg, visible);

        if (typeRef.TypeArgs.Count == 0)
            return;

        if (!ctx.TypeTable.TryResolve(typeRef.Name.Full, out var resolvedDecl) || resolvedDecl is null)
            return;

        var typeParams = resolvedDecl.TypeParams;
        for (var i = 0; i < Math.Min(typeRef.TypeArgs.Count, typeParams.Count); i++)
        {
            var constraint = typeParams[i].Constraint;
            if (constraint is null)
                continue;

            var argRef = typeRef.TypeArgs[i];
            if (visible.Any(tp => tp.Name == argRef.Name.Full))
                continue;

            if (!ctx.TypeTable.TryResolve(argRef.Name.Full, out var argDecl) || argDecl is null)
                continue;

            if (SatisfiesConstraint(argDecl, constraint, ctx))
                continue;

            ctx.AddError(
                $"Type argument '{argRef.Name.Full}' does not satisfy the constraint '{constraint}' on type parameter '{typeParams[i].Name}' of '{resolvedDecl.QualifiedName ?? resolvedDecl.Name}'.",
                file.FileName);
        }
    }
}
