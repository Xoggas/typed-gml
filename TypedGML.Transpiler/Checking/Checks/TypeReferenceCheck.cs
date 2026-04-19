using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking.Checks;

/// <summary>
///     Validates that every referenced type exists and reports legacy numeric aliases
///     so the language consistently uses <c>number</c> instead of <c>int</c> / <c>real</c>.
/// </summary>
public sealed class TypeReferenceCheck : AstBodyWalker
{
    public override string Name => "TypeReferenceCheck";

    protected override void OnEnterType(TranspileContext ctx, TgmlFile file, TgmlTypeDecl decl)
    {
        foreach (var tp in decl.TypeParams)
            if (tp.Constraint is not null)
                ValidateTypeRef(ctx, file, tp.Constraint, decl.TypeParams);

        switch (decl)
        {
            case TgmlClassDecl cls:
                foreach (var bt in cls.BaseTypes)
                    ValidateTypeRef(ctx, file, bt, cls.TypeParams);
                foreach (var field in cls.Fields)
                    ValidateTypeRef(ctx, file, field.Type, cls.TypeParams);
                foreach (var prop in cls.Properties)
                {
                    ValidateTypeRef(ctx, file, prop.Type, cls.TypeParams);
                    if (prop.IndexParam is not null)
                        ValidateTypeRef(ctx, file, prop.IndexParam.Type, cls.TypeParams);
                }
                break;

            case TgmlStructDecl str:
                foreach (var bt in str.BaseTypes)
                    ValidateTypeRef(ctx, file, bt, str.TypeParams);
                foreach (var field in str.Fields)
                    ValidateTypeRef(ctx, file, field.Type, str.TypeParams);
                foreach (var prop in str.Properties)
                {
                    ValidateTypeRef(ctx, file, prop.Type, str.TypeParams);
                    if (prop.IndexParam is not null)
                        ValidateTypeRef(ctx, file, prop.IndexParam.Type, str.TypeParams);
                }
                break;

            case TgmlInterfaceDecl iface:
                foreach (var bt in iface.BaseInterfaces)
                    ValidateTypeRef(ctx, file, bt, iface.TypeParams);
                foreach (var method in iface.Methods)
                {
                    var visible = iface.TypeParams.Concat(method.TypeParams).ToList();
                    ValidateTypeRef(ctx, file, method.ReturnType, visible);
                    foreach (var tp in method.TypeParams)
                        if (tp.Constraint is not null)
                            ValidateTypeRef(ctx, file, tp.Constraint, visible);
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

    protected override void OnEnterCallable(TranspileContext ctx, TgmlFile file, WalkContext wctx)
    {
        var visible = VisibleTypeParams(wctx);

        if (wctx.Member is TgmlMethodDecl method)
        {
            ValidateTypeRef(ctx, file, method.ReturnType, visible);
            foreach (var tp in method.TypeParams)
                if (tp.Constraint is not null)
                    ValidateTypeRef(ctx, file, tp.Constraint, visible);
        }

        foreach (var param in wctx.Params)
            ValidateTypeRef(ctx, file, param.Type, visible);
    }

    protected override bool OnStatement(TranspileContext ctx, TgmlFile file, TgmlStatement stmt, WalkContext wctx)
    {
        var visible = VisibleTypeParams(wctx);

        switch (stmt)
        {
            case TgmlLocalVarDecl local:
                if (ValidateImplicitLocalTypeUsage(ctx, file, local.Type, local.Line, local.Column))
                    break;
                ValidateTypeRef(ctx, file, local.Type, visible, local.Line, local.Column);
                break;

            case TgmlForStmt { Init: TgmlForInitDecl initDecl } forStmt:
                if (ValidateImplicitLocalTypeUsage(ctx, file, initDecl.Type, forStmt.Line, forStmt.Column))
                    break;
                ValidateTypeRef(ctx, file, initDecl.Type, visible, forStmt.Line, forStmt.Column);
                break;

            case TgmlWithStmt withStmt:
                ValidateTypeRef(ctx, file, withStmt.IterType, visible, withStmt.Line, withStmt.Column);
                break;

            case TgmlTryStmt tryStmt:
                foreach (var catchClause in tryStmt.CatchClauses)
                    ValidateTypeRef(ctx, file, catchClause.ExceptionType, visible, tryStmt.Line, tryStmt.Column);
                break;
        }

        return true;
    }

    protected override void OnExpression(TranspileContext ctx, TgmlFile file, TgmlExpression expr, WalkContext wctx)
    {
        var visible = VisibleTypeParams(wctx);

        switch (expr)
        {
            case TgmlCastExpr cast:
                ValidateTypeRef(ctx, file, cast.TargetType, visible, cast.Line, cast.Column);
                break;

            case TgmlNewObjectExpr newObject:
                ValidateTypeRef(ctx, file, newObject.Type, visible, newObject.Line, newObject.Column);
                break;

            case TgmlNewArrayExpr newArray:
                ValidateTypeRef(ctx, file, newArray.ElementType, visible, newArray.Line, newArray.Column);
                break;

            case TgmlTypeofExpr typeofExpr:
                ValidateTypeRef(ctx, file, typeofExpr.Type, visible, typeofExpr.Line, typeofExpr.Column);
                break;

            case TgmlDefaultExpr { Type: { } defaultType } defaultExpr:
                ValidateTypeRef(ctx, file, defaultType, visible, defaultExpr.Line, defaultExpr.Column);
                break;

            case TgmlIsExpr isExpr:
                ValidateTypeRef(ctx, file, isExpr.CheckType, visible, isExpr.Line, isExpr.Column);
                break;

            case TgmlAsExpr asExpr:
                ValidateTypeRef(ctx, file, asExpr.TargetType, visible, asExpr.Line, asExpr.Column);
                break;

            case TgmlLambdaExpr lambdaExpr:
                foreach (var param in lambdaExpr.Params)
                    if (param.Type.Name.Full != "?")
                        ValidateTypeRef(ctx, file, param.Type, visible, lambdaExpr.Line, lambdaExpr.Column);
                break;
        }
    }

    private static IReadOnlyList<TgmlTypeParam> VisibleTypeParams(WalkContext wctx)
    {
        var visible = new List<TgmlTypeParam>(wctx.OwnerType.TypeParams);

        if (wctx.Member is TgmlMethodDecl method)
            visible.AddRange(method.TypeParams);

        return visible;
    }

    private static void ValidateTypeRef(
        TranspileContext ctx,
        TgmlFile file,
        TgmlTypeRef typeRef,
        IReadOnlyList<TgmlTypeParam> visibleTypeParams,
        int line = 0,
        int column = 0)
    {
        foreach (var arg in typeRef.TypeArgs)
            ValidateTypeRef(ctx, file, arg, visibleTypeParams, line, column);

        var name = typeRef.Name.Full;

        if (visibleTypeParams.Any(tp => tp.Name == name))
            return;

        if (BuiltinTypeFacts.IsLegacyNumericAlias(name))
        {
            ctx.AddError(
                $"Type '{name}' has been replaced by 'number'.",
                file.FileName,
                line,
                column);
            return;
        }

        if (BuiltinTypeFacts.IsBuiltIn(name))
            return;

        if (ctx.TypeTable.TryResolve(name, typeRef.TypeArgs.Count, out var resolvedDecl) && resolvedDecl is not null)
            return;

        ctx.AddError(
            $"Unknown type '{name}'.",
            file.FileName,
            line,
            column);
    }

    private static bool ValidateImplicitLocalTypeUsage(
        TranspileContext ctx,
        TgmlFile file,
        TgmlTypeRef typeRef,
        int line,
        int column)
    {
        if (typeRef.Name.Full != "var")
            return false;

        if (typeRef.ArrayDepth > 0 || typeRef.TypeArgs.Count > 0)
        {
            ctx.AddError(
                "Implicitly typed local variables cannot use array specifiers or type arguments.",
                file.FileName,
                line,
                column);
        }

        return true;
    }
}
