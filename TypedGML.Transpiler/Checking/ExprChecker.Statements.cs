using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking;

public sealed partial class ExprChecker
{
    public void CheckBlock(TgmlBlock block)
    {
        Symbols.PushScope();
        foreach (var stmt in block.Statements)
            CheckStmt(stmt);
        Symbols.PopScope();
    }

    public void CheckStmt(TgmlStatement stmt)
    {
        switch (stmt)
        {
            case TgmlBlock b:
                CheckBlock(b);
                break;
            case TgmlLocalVarDecl v:
                if (v.IsImplicitlyTyped)
                    DefineImplicitLocal(v.Name, v.Initializer, v.Line, v.Column, "local variable");
                else
                {
                    if (v.Initializer is TgmlNewImplicitExpr ni)
                        ni.Metadata["InferredType"] = DefaultExpressionFacts.DescribeType(v.Type);
                    if (v.Initializer is not null)
                    {
                        CheckAssignCompatibility(DefaultExpressionFacts.DescribeType(v.Type), v.Initializer, v.Line, v.Column,
                            $"Cannot assign '{{1}}' to variable of type '{v.Type.Name.Full}'.");
                    }

                    Symbols.Define(v.Name, v.Type);
                }
                break;
            case TgmlExpressionStmt es:
                CheckExpr(es.Expression);
                break;
            case TgmlIfStmt i:
                foreach (var br in i.Branches)
                {
                    CheckExpr(br.Condition);
                    CheckBlock(br.Body);
                }

                if (i.ElseBlock is not null)
                    CheckBlock(i.ElseBlock);
                break;
            case TgmlWhileStmt w:
                CheckExpr(w.Condition);
                CheckBlock(w.Body);
                break;
            case TgmlForStmt f:
                Symbols.PushScope();
                switch (f.Init)
                {
                    case TgmlForInitDecl d:
                        if (d.IsImplicitlyTyped)
                            DefineImplicitLocal(d.Name, d.Initializer, f.Line, f.Column, "for-loop variable");
                        else
                        {
                            if (d.Initializer is not null)
                            {
                                CheckAssignCompatibility(DefaultExpressionFacts.DescribeType(d.Type), d.Initializer, f.Line, f.Column,
                                    $"Cannot assign '{{1}}' to variable of type '{d.Type.Name.Full}'.");
                            }

                            Symbols.Define(d.Name, d.Type);
                        }
                        break;
                    case TgmlForInitExpr e:
                        CheckExpr(e.Expression);
                        break;
                }

                if (f.Condition is not null)
                    CheckExpr(f.Condition);
                foreach (var update in f.Updates)
                    CheckExpr(update);
                foreach (var bodyStmt in f.Body.Statements)
                    CheckStmt(bodyStmt);
                Symbols.PopScope();
                break;
            case TgmlRepeatStmt r:
                CheckExpr(r.Count);
                CheckBlock(r.Body);
                break;
            case TgmlSwitchStmt sw:
                CheckExpr(sw.Value);
                foreach (var sec in sw.Sections)
                {
                    if (sec.CaseValue is not null)
                        CheckExpr(sec.CaseValue);
                    foreach (var sectionStmt in sec.Statements)
                        CheckStmt(sectionStmt);
                }
                break;
            case TgmlWithStmt withStmt:
                Symbols.PushScope();
                Symbols.Define(withStmt.VarName, withStmt.IterType);
                CheckBlock(withStmt.Body);
                Symbols.PopScope();
                break;
            case TgmlReturnStmt ret:
                CheckReturn(ret);
                break;
            case TgmlTryStmt tr:
                CheckBlock(tr.TryBlock);
                foreach (var cc in tr.CatchClauses)
                {
                    var inner = new ExprChecker(_ctx, _file, Symbols, _owner, _returnType);
                    inner.Symbols.PushScope();
                    inner.Symbols.Define(cc.VarName, cc.ExceptionType);
                    foreach (var catchStmt in cc.Body.Statements)
                        inner.CheckStmt(catchStmt);
                    inner.Symbols.PopScope();
                }

                if (tr.FinallyBlock is not null)
                    CheckBlock(tr.FinallyBlock);
                break;
        }
    }

    private void CheckReturn(TgmlReturnStmt ret)
    {
        if (_returnType == "void")
        {
            if (ret.Value is not null)
            {
                CheckExpr(ret.Value);
                Error(ret.Line, ret.Column, "Cannot return a value from a void method.");
            }

            return;
        }

        if (ret.Value is null)
        {
            _ctx.AddWarning($"Missing return value; method declares return type '{_returnType}'.", _file.FileName, ret.Line, ret.Column);
            return;
        }

        CheckAssignCompatibility(_returnType, ret.Value, ret.Line, ret.Column,
            $"Return type mismatch: cannot convert '{{1}}' to '{_returnType}'.");
    }
}
