using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking.Checks;

/// <summary>
///     Batch 4: Warns when statements follow an unconditional terminator
///     (<c>return</c>, <c>break</c>, <c>continue</c>) within the same block.
/// </summary>
public sealed class UnreachableCodeCheck : AstBodyWalker
{
    public override string Name => "UnreachableCodeCheck";

    // Each entry is "did we see a terminator in this block so far?"
    private readonly Stack<bool> _terminatorSeen = new();

    protected override void OnEnterCallable(TranspileContext ctx, TgmlFile file, WalkContext wctx)
        => _terminatorSeen.Clear();

    protected override void OnEnterBlock(TranspileContext ctx, TgmlFile file,
        TgmlBlock block, WalkContext wctx)
        => _terminatorSeen.Push(false);

    protected override void OnLeaveBlock(TranspileContext ctx, TgmlFile file,
        TgmlBlock block, WalkContext wctx)
    {
        if (_terminatorSeen.Count > 0) _terminatorSeen.Pop();
    }

    protected override bool OnStatement(TranspileContext ctx, TgmlFile file,
        TgmlStatement stmt, WalkContext wctx)
    {
        if (_terminatorSeen.TryPeek(out var seen) && seen)
        {
            ctx.AddWarning("Unreachable code detected.", file.FileName, stmt.Line, stmt.Column);
            return false; // skip children of unreachable statement
        }

        if (stmt is TgmlReturnStmt or TgmlBreakStmt or TgmlContinueStmt)
            SetTerminator();

        return true;
    }

    private void SetTerminator()
    {
        if (_terminatorSeen.Count > 0)
        {
            _terminatorSeen.Pop();
            _terminatorSeen.Push(true);
        }
    }
}

