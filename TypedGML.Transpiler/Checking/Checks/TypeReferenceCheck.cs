using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking.Checks;

public sealed partial class TypeReferenceCheck : AstBodyWalker
{
    public override string Name => "TypeReferenceCheck";

    protected override void OnEnterType(TranspileContext ctx, TgmlFile file, TgmlTypeDecl decl)
    {
        foreach (var tp in decl.TypeParams)
        {
            if (tp.Constraint is not null)
                ValidateTypeRef(ctx, file, tp.Constraint, decl.TypeParams);
        }

        ValidateTypeDeclaration(ctx, file, decl);
    }

    protected override void OnEnterCallable(TranspileContext ctx, TgmlFile file, WalkContext wctx)
    {
        var visible = VisibleTypeParams(wctx);

        if (wctx.Member is TgmlMethodDecl method)
        {
            ValidateTypeRef(ctx, file, method.ReturnType, visible);
            foreach (var tp in method.TypeParams)
            {
                if (tp.Constraint is not null)
                    ValidateTypeRef(ctx, file, tp.Constraint, visible);
            }
        }

        foreach (var param in wctx.Params)
            ValidateTypeRef(ctx, file, param.Type, visible);
    }
}
