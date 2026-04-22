using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking.Checks;

public sealed partial class TypeReferenceCheck
{
    private static void ValidateTypeDeclaration(TranspileContext ctx, TgmlFile file, TgmlTypeDecl decl)
    {
        switch (decl)
        {
            case TgmlClassDecl cls:
                ValidateCompositeType(ctx, file, cls.BaseTypes, cls.Fields, cls.Properties, cls.TypeParams);
                break;
            case TgmlStructDecl str:
                ValidateCompositeType(ctx, file, str.BaseTypes, str.Fields, str.Properties, str.TypeParams);
                break;
            case TgmlInterfaceDecl iface:
                foreach (var baseInterface in iface.BaseInterfaces)
                    ValidateTypeRef(ctx, file, baseInterface, iface.TypeParams);

                foreach (var method in iface.Methods)
                {
                    var visible = iface.TypeParams.Concat(method.TypeParams).ToList();
                    ValidateTypeRef(ctx, file, method.ReturnType, visible);
                    foreach (var tp in method.TypeParams)
                    {
                        if (tp.Constraint is not null)
                            ValidateTypeRef(ctx, file, tp.Constraint, visible);
                    }

                    foreach (var param in method.Params)
                        ValidateTypeRef(ctx, file, param.Type, visible);
                }

                foreach (var prop in iface.Properties)
                    ValidateTypeRef(ctx, file, prop.Type, iface.TypeParams);
                break;
            case TgmlDelegateDecl dlg:
                ValidateTypeRef(ctx, file, dlg.ReturnType, dlg.TypeParams);
                foreach (var param in dlg.Params)
                    ValidateTypeRef(ctx, file, param.Type, dlg.TypeParams);
                break;
        }
    }

    private static void ValidateCompositeType(
        TranspileContext ctx,
        TgmlFile file,
        IEnumerable<TgmlTypeRef> baseTypes,
        IEnumerable<TgmlFieldDecl> fields,
        IEnumerable<TgmlPropertyDecl> properties,
        IReadOnlyList<TgmlTypeParam> visibleTypeParams)
    {
        foreach (var baseType in baseTypes)
            ValidateTypeRef(ctx, file, baseType, visibleTypeParams);
        foreach (var field in fields)
            ValidateTypeRef(ctx, file, field.Type, visibleTypeParams);
        foreach (var prop in properties)
        {
            ValidateTypeRef(ctx, file, prop.Type, visibleTypeParams);
            if (prop.IndexParam is not null)
                ValidateTypeRef(ctx, file, prop.IndexParam.Type, visibleTypeParams);
        }
    }
}
