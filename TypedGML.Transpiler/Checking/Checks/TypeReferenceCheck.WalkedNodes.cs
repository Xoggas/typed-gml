using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking.Checks;

public sealed partial class TypeReferenceCheck
{
    protected override bool OnStatement(TranspileContext ctx, TgmlFile file, TgmlStatement stmt, WalkContext wctx)
    {
        var visible = VisibleTypeParams(wctx);

        switch (stmt)
        {
            case TgmlLocalVarDecl local:
                if (!ValidateImplicitLocalTypeUsage(ctx, file, local.Type, local.Line, local.Column))
                    ValidateTypeRef(ctx, file, local.Type, visible, local.Line, local.Column);
                break;
            case TgmlForStmt { Init: TgmlForInitDecl initDecl } forStmt:
                if (!ValidateImplicitLocalTypeUsage(ctx, file, initDecl.Type, forStmt.Line, forStmt.Column))
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
                {
                    if (param.Type.Name.Full != "?")
                        ValidateTypeRef(ctx, file, param.Type, visible, lambdaExpr.Line, lambdaExpr.Column);
                }
                break;
        }
    }
}
