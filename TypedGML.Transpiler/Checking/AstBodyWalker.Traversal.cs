using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking;

public abstract partial class AstBodyWalker
{
    protected void WalkBlock(TranspileContext ctx, TgmlFile file, TgmlBlock block, WalkContext wctx)
    {
        OnEnterBlock(ctx, file, block, wctx);
        foreach (var stmt in block.Statements)
            WalkStmt(ctx, file, stmt, wctx);
        OnLeaveBlock(ctx, file, block, wctx);
    }

    protected void WalkStmt(TranspileContext ctx, TgmlFile file, TgmlStatement stmt, WalkContext wctx)
    {
        if (!OnStatement(ctx, file, stmt, wctx))
            return;

        switch (stmt)
        {
            case TgmlBlock b:
                WalkBlock(ctx, file, b, wctx);
                break;
            case TgmlLocalVarDecl v:
                if (v.Initializer is not null)
                    WalkExpr(ctx, file, v.Initializer, wctx);
                break;
            case TgmlExpressionStmt es:
                WalkExpr(ctx, file, es.Expression, wctx);
                break;
            case TgmlIfStmt i:
                foreach (var branch in i.Branches)
                {
                    WalkExpr(ctx, file, branch.Condition, wctx);
                    WalkBlock(ctx, file, branch.Body, wctx);
                }

                if (i.ElseBlock is not null)
                    WalkBlock(ctx, file, i.ElseBlock, wctx);
                break;
            case TgmlWhileStmt w:
                WalkExpr(ctx, file, w.Condition, wctx);
                WalkBlock(ctx, file, w.Body, wctx);
                break;
            case TgmlForStmt f:
                if (f.Init is TgmlForInitDecl fd && fd.Initializer is not null)
                    WalkExpr(ctx, file, fd.Initializer, wctx);
                else if (f.Init is TgmlForInitExpr fe)
                    WalkExpr(ctx, file, fe.Expression, wctx);

                if (f.Condition is not null)
                    WalkExpr(ctx, file, f.Condition, wctx);
                foreach (var update in f.Updates)
                    WalkExpr(ctx, file, update, wctx);
                WalkBlock(ctx, file, f.Body, wctx);
                break;
            case TgmlRepeatStmt r:
                WalkExpr(ctx, file, r.Count, wctx);
                WalkBlock(ctx, file, r.Body, wctx);
                break;
            case TgmlSwitchStmt sw:
                WalkExpr(ctx, file, sw.Value, wctx);
                foreach (var sec in sw.Sections)
                {
                    if (sec.CaseValue is not null)
                        WalkExpr(ctx, file, sec.CaseValue, wctx);
                    foreach (var sectionStmt in sec.Statements)
                        WalkStmt(ctx, file, sectionStmt, wctx);
                }
                break;
            case TgmlWithStmt withStmt:
                WalkBlock(ctx, file, withStmt.Body, wctx);
                break;
            case TgmlReturnStmt ret:
                if (ret.Value is not null)
                    WalkExpr(ctx, file, ret.Value, wctx);
                break;
            case TgmlTryStmt tr:
                WalkBlock(ctx, file, tr.TryBlock, wctx);
                foreach (var cc in tr.CatchClauses)
                    WalkBlock(ctx, file, cc.Body, wctx);
                if (tr.FinallyBlock is not null)
                    WalkBlock(ctx, file, tr.FinallyBlock, wctx);
                break;
        }

        AfterStatement(ctx, file, stmt, wctx);
    }

    protected void WalkExpr(TranspileContext ctx, TgmlFile file, TgmlExpression expr, WalkContext wctx)
    {
        OnExpression(ctx, file, expr, wctx);

        switch (expr)
        {
            case TgmlBinaryExpr b:
                WalkExpr(ctx, file, b.Left, wctx);
                WalkExpr(ctx, file, b.Right, wctx);
                break;
            case TgmlUnaryExpr u:
                WalkExpr(ctx, file, u.Operand, wctx);
                break;
            case TgmlAssignExpr a:
                WalkExpr(ctx, file, a.Target, wctx);
                WalkExpr(ctx, file, a.Value, wctx);
                break;
            case TgmlTernaryExpr t:
                WalkExpr(ctx, file, t.Condition, wctx);
                WalkExpr(ctx, file, t.ThenExpr, wctx);
                WalkExpr(ctx, file, t.ElseExpr, wctx);
                break;
            case TgmlMethodCallExpr mc:
                WalkExpr(ctx, file, mc.Target, wctx);
                foreach (var arg in mc.Args)
                    WalkExpr(ctx, file, arg.Value, wctx);
                break;
            case TgmlFuncCallExpr fc:
                foreach (var arg in fc.Args)
                    WalkExpr(ctx, file, arg.Value, wctx);
                break;
            case TgmlFieldAccessExpr fa:
                WalkExpr(ctx, file, fa.Target, wctx);
                break;
            case TgmlIndexExpr ix:
                WalkExpr(ctx, file, ix.Target, wctx);
                WalkExpr(ctx, file, ix.Index, wctx);
                break;
            case TgmlInvokeExpr invoke:
                WalkExpr(ctx, file, invoke.Target, wctx);
                foreach (var arg in invoke.Args)
                    WalkExpr(ctx, file, arg.Value, wctx);
                break;
            case TgmlParenExpr p:
                WalkExpr(ctx, file, p.Inner, wctx);
                break;
            case TgmlCastExpr c:
                WalkExpr(ctx, file, c.Operand, wctx);
                break;
            case TgmlNewObjectExpr n:
                foreach (var arg in n.Args)
                    WalkExpr(ctx, file, arg.Value, wctx);
                break;
            case TgmlBaseCallExpr bc:
                foreach (var arg in bc.Args)
                    WalkExpr(ctx, file, arg.Value, wctx);
                break;
            case TgmlNewArrayExpr na:
                WalkExpr(ctx, file, na.Size, wctx);
                break;
            case TgmlArrayInitExpr ai:
                foreach (var element in ai.Elements)
                    WalkExpr(ctx, file, element, wctx);
                break;
            case TgmlDictionaryInitExpr di:
                foreach (var entry in di.Entries)
                {
                    WalkExpr(ctx, file, entry.Key, wctx);
                    WalkExpr(ctx, file, entry.Value, wctx);
                }
                break;
            case TgmlLambdaExpr lam:
                if (lam.ExprBody is not null)
                    WalkExpr(ctx, file, lam.ExprBody, wctx);
                else if (lam.BlockBody is not null)
                    WalkBlock(ctx, file, lam.BlockBody, wctx);
                break;
        }
    }
}
