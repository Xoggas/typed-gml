using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Generation;

public sealed partial class StatementEmitter
{
    private void EmitIf(TgmlIfStmt s, GmlWriter w)
    {
        for (var i = 0; i < s.Branches.Count; i++)
        {
            var branch = s.Branches[i];
            var keyword = i == 0 ? "if" : "else if";
            w.WriteLine($"{keyword} ({_expr.Emit(branch.Condition)})");
            w.OpenBrace();
            EmitBlock(branch.Body, w);
            w.CloseBrace();
        }

        if (s.ElseBlock is null)
            return;

        w.WriteLine("else");
        w.OpenBrace();
        EmitBlock(s.ElseBlock, w);
        w.CloseBrace();
    }

    private void EmitWhile(TgmlWhileStmt s, GmlWriter w)
    {
        w.WriteLine($"while ({_expr.Emit(s.Condition)})");
        w.OpenBrace();
        EmitBlock(s.Body, w);
        w.CloseBrace();
    }

    private void EmitFor(TgmlForStmt s, GmlWriter w)
    {
        var init = s.Init switch
        {
            TgmlForInitDecl d => d.Initializer is not null ? $"var {d.Name} = {_expr.Emit(d.Initializer)}" : $"var {d.Name}",
            TgmlForInitExpr e => _expr.Emit(e.Expression),
            _ => string.Empty
        };
        var cond = s.Condition is not null ? _expr.Emit(s.Condition) : string.Empty;
        var updates = string.Join(", ", s.Updates.Select(_expr.Emit));

        w.WriteLine($"for ({init}; {cond}; {updates})");
        w.OpenBrace();
        EmitBlock(s.Body, w);
        w.CloseBrace();
    }

    private void EmitRepeat(TgmlRepeatStmt s, GmlWriter w)
    {
        w.WriteLine($"repeat ({_expr.Emit(s.Count)})");
        w.OpenBrace();
        EmitBlock(s.Body, w);
        w.CloseBrace();
    }

    private void EmitSwitch(TgmlSwitchStmt s, GmlWriter w)
    {
        w.WriteLine($"switch ({_expr.Emit(s.Value)})");
        w.OpenBrace();

        foreach (var sec in s.Sections)
        {
            w.WriteLine(sec.IsDefault ? "default:" : $"case {_expr.Emit(sec.CaseValue!)}:");
            w.Indent();
            foreach (var stmt in sec.Statements)
                Emit(stmt, w);
            w.Dedent();
        }

        w.CloseBrace();
    }

    private void EmitWith(TgmlWithStmt s, GmlWriter w)
    {
        var typeName = s.IterType.Name.Full;
        _ctx.TypeTable.TryResolve(typeName, out var td);
        var withTarget = td is TgmlClassDecl cls && cls.IsGameObject ? _ctx.GmlObjectName(cls) : s.VarName;

        var previousAlias = _ctx.WithAlias;
        _ctx.WithAlias = s.VarName;
        w.WriteLine($"with ({withTarget})");
        w.OpenBrace();
        EmitBlock(s.Body, w);
        w.CloseBrace();
        _ctx.WithAlias = previousAlias;
    }

    private void EmitReturn(TgmlReturnStmt s, GmlWriter w)
    {
        if (s.Value is TgmlBaseCallExpr baseCall && TryEmitInlinedGameObjectBaseCall(baseCall, w))
            return;

        w.WriteLine(s.Value is not null ? $"return {_expr.Emit(s.Value)};" : "return;");
    }

    private void EmitTry(TgmlTryStmt s, GmlWriter w)
    {
        w.WriteLine("try");
        w.OpenBrace();
        EmitBlock(s.TryBlock, w);
        w.CloseBrace();

        foreach (var c in s.CatchClauses)
        {
            w.WriteLine($"catch ({c.VarName})");
            w.OpenBrace();
            EmitBlock(c.Body, w);
            w.CloseBrace();
        }

        if (s.FinallyBlock is null)
            return;

        w.WriteLine("finally");
        w.OpenBrace();
        EmitBlock(s.FinallyBlock, w);
        w.CloseBrace();
    }
}
