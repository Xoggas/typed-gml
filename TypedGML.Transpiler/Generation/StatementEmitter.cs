using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Generation;

public sealed partial class StatementEmitter
{
    private readonly GenerationContext _ctx;
    private readonly ExpressionEmitter _expr;

    public StatementEmitter(GenerationContext ctx)
    {
        _ctx = ctx;
        _expr = new ExpressionEmitter(ctx);
    }

    public void Emit(TgmlStatement stmt, GmlWriter w)
    {
        switch (stmt)
        {
            case TgmlBlock s: EmitBlock(s, w); break;
            case TgmlLocalVarDecl s: EmitLocalVar(s, w); break;
            case TgmlExpressionStmt s: EmitExprStmt(s, w); break;
            case TgmlIfStmt s: EmitIf(s, w); break;
            case TgmlWhileStmt s: EmitWhile(s, w); break;
            case TgmlForStmt s: EmitFor(s, w); break;
            case TgmlRepeatStmt s: EmitRepeat(s, w); break;
            case TgmlSwitchStmt s: EmitSwitch(s, w); break;
            case TgmlWithStmt s: EmitWith(s, w); break;
            case TgmlReturnStmt s: EmitReturn(s, w); break;
            case TgmlBreakStmt _: w.WriteLine("break;"); break;
            case TgmlContinueStmt _: w.WriteLine("continue;"); break;
            case TgmlTryStmt s: EmitTry(s, w); break;
            case TgmlRawStmt s: w.WriteLine(s.RawText[1..].Trim()); break;
            default:
                w.WriteLine($"// unsupported statement: {stmt.GetType().Name}");
                break;
        }
    }

    public void EmitBlock(TgmlBlock block, GmlWriter w)
    {
        _ctx.PushLocalScope([]);
        foreach (var stmt in block.Statements)
            Emit(stmt, w);
        _ctx.PopLocalScope();
    }

    private void EmitLocalVar(TgmlLocalVarDecl s, GmlWriter w)
    {
        _ctx.DeclareLocal(s.Name);
        if (s.Initializer is not null)
        {
            if (s.Initializer is TgmlNewObjectExpr newObj &&
                _ctx.TypeTable.TryResolve(newObj.Type.Name.Full, out var td) &&
                td is TgmlClassDecl objCls && objCls.IsGameObject)
            {
                EmitGameObjectCreation(s.Name, newObj, objCls, w);
                return;
            }

            w.WriteLine($"var {s.Name} = {_expr.Emit(s.Initializer)};");
            return;
        }

        w.WriteLine($"var {s.Name};");
    }

    private void EmitExprStmt(TgmlExpressionStmt s, GmlWriter w)
    {
        if (s.Expression is TgmlNewObjectExpr newObj &&
            _ctx.TypeTable.TryResolve(newObj.Type.Name.Full, out var td) &&
            td is TgmlClassDecl objCls && objCls.IsGameObject)
        {
            EmitGameObjectCreation("__inst", newObj, objCls, w);
            return;
        }

        if (s.Expression is TgmlBaseCallExpr baseCall && TryEmitInlinedGameObjectBaseCall(baseCall, w))
            return;

        w.WriteLine($"{_expr.Emit(s.Expression)};");
    }
}
