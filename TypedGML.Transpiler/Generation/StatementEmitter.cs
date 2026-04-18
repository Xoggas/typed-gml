using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Generation;

/// <summary>
///     Converts TypedGML statement nodes into GML code via a <see cref="GmlWriter" />.
/// </summary>
public sealed class StatementEmitter
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
            case TgmlRawStmt s: w.WriteLine(s.RawText[1..].Trim()); break; // strip '#'
            default:
                w.WriteLine($"// unsupported statement: {stmt.GetType().Name}");
                break;
        }
    }

    public void EmitBlock(TgmlBlock block, GmlWriter w)
    {
        foreach (var stmt in block.Statements)
        {
            Emit(stmt, w);
        }
    }

    private void EmitLocalVar(TgmlLocalVarDecl s, GmlWriter w)
    {
        if (s.Initializer is not null)
        {
            // Special case: var e = new @ObjectClass(...) → split into two statements
            if (s.Initializer is TgmlNewObjectExpr newObj &&
                _ctx.TypeTable.TryResolve(newObj.Type.Name.Full, out var td) &&
                td is TgmlClassDecl objCls && objCls.IsGameObject)
            {
                EmitGameObjectCreation(s.Name, newObj, objCls, w);
                return;
            }

            w.WriteLine($"var {s.Name} = {_expr.Emit(s.Initializer)};");
        }
        else
        {
            w.WriteLine($"var {s.Name};");
        }
    }

    private void EmitGameObjectCreation(string varName, TgmlNewObjectExpr newObj, TgmlClassDecl cls, GmlWriter w)
    {
        var objGml = _ctx.GmlObjectName(cls);
        var args = newObj.Args.Select(_expr.Emit).ToList();
        var baseArgCount = cls.Constructor?.BaseArgs?.Count ?? 0;
        var createArgs = args.Take(Math.Max(3, baseArgCount)).ToList();
        var initArgs = args.Skip(Math.Max(3, baseArgCount)).ToList();

        // Ensure 3 positional args for instance_create_layer
        while (createArgs.Count < 3)
        {
            createArgs.Add("0");
        }

        w.WriteLine($"var {varName} = instance_create_layer({string.Join(", ", createArgs)}, {objGml});");

        if (initArgs.Count > 0)
        {
            w.WriteLine($"{objGml}_Init({varName}, {string.Join(", ", initArgs)});");
        }
    }

    private void EmitExprStmt(TgmlExpressionStmt s, GmlWriter w)
    {
        // Special case: standalone new @ObjectClass(...)
        if (s.Expression is TgmlNewObjectExpr newObj &&
            _ctx.TypeTable.TryResolve(newObj.Type.Name.Full, out var td) &&
            td is TgmlClassDecl objCls && objCls.IsGameObject)
        {
            EmitGameObjectCreation("__inst", newObj, objCls, w);
            return;
        }

        w.WriteLine($"{_expr.Emit(s.Expression)};");
    }

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

        if (s.ElseBlock is not null)
        {
            w.WriteLine("else");
            w.OpenBrace();
            EmitBlock(s.ElseBlock, w);
            w.CloseBrace();
        }
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
            TgmlForInitDecl d => d.Initializer is not null
                ? $"var {d.Name} = {_expr.Emit(d.Initializer)}"
                : $"var {d.Name}",
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
            if (sec.IsDefault)
            {
                w.WriteLine("default:");
            }
            else
            {
                w.WriteLine($"case {_expr.Emit(sec.CaseValue!)}:");
            }

            w.Indent();
            foreach (var stmt in sec.Statements)
            {
                Emit(stmt, w);
            }

            w.Dedent();
        }

        w.CloseBrace();
    }

    private void EmitWith(TgmlWithStmt s, GmlWriter w)
    {
        // Resolve the GML type name for the with target
        var typeName = s.IterType.Name.Full;
        _ctx.TypeTable.TryResolve(typeName, out var td);
        string gmlType;
        if (td is TgmlClassDecl cls && cls.IsGameObject)
        {
            gmlType = _ctx.GmlObjectName(cls);
        }
        else
        {
            gmlType = s.IterType.GmlBaseName;
        }

        var prev = _ctx.WithAlias;
        _ctx.WithAlias = s.VarName;
        w.WriteLine($"with ({gmlType})");
        w.OpenBrace();
        EmitBlock(s.Body, w);
        w.CloseBrace();
        _ctx.WithAlias = prev;
    }

    private void EmitReturn(TgmlReturnStmt s, GmlWriter w)
    {
        if (s.Value is not null)
        {
            w.WriteLine($"return {_expr.Emit(s.Value)};");
        }
        else
        {
            w.WriteLine("return;");
        }
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

        if (s.FinallyBlock is not null)
        {
            w.WriteLine("finally");
            w.OpenBrace();
            EmitBlock(s.FinallyBlock, w);
            w.CloseBrace();
        }
    }
}