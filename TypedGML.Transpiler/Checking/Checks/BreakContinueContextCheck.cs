using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking.Checks;

/// <summary>
///     Batch 4: Reports an error when <c>break</c> or <c>continue</c> appear outside
///     the loop / switch context that gives them meaning.
/// </summary>
public sealed class BreakContinueContextCheck : AstBodyWalker
{
    public override string Name => "BreakContinueContextCheck";

    private int _loopDepth;
    private int _switchDepth;

    protected override void OnEnterCallable(TranspileContext ctx, TgmlFile file, WalkContext wctx)
    {
        _loopDepth = 0;
        _switchDepth = 0;
    }

    protected override bool OnStatement(TranspileContext ctx, TgmlFile file,
        TgmlStatement stmt, WalkContext wctx)
    {
        switch (stmt)
        {
            case TgmlBreakStmt brk:
                if (_loopDepth == 0 && _switchDepth == 0)
                    ctx.AddError("'break' is only valid inside a loop or switch statement.",
                        file.FileName, brk.Line, brk.Column);
                break;

            case TgmlContinueStmt cont:
                if (_loopDepth == 0)
                    ctx.AddError("'continue' is only valid inside a loop.",
                        file.FileName, cont.Line, cont.Column);
                break;

            case TgmlWhileStmt or TgmlForStmt or TgmlRepeatStmt:
                _loopDepth++;
                break;

            case TgmlSwitchStmt:
                _switchDepth++;
                break;
        }

        return true;
    }

    protected override void AfterStatement(TranspileContext ctx, TgmlFile file,
        TgmlStatement stmt, WalkContext wctx)
    {
        switch (stmt)
        {
            case TgmlWhileStmt or TgmlForStmt or TgmlRepeatStmt:
                _loopDepth--;
                break;

            case TgmlSwitchStmt:
                _switchDepth--;
                break;
        }
    }
}

