using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking;

/// <summary>
///     Stateful, symbol-table-aware expression type-checker.
///     Walks a callable body tracking local variable types and verifying:
///     <list type="bullet">
///         <item>Binary / unary operator operand compatibility</item>
///         <item>Assignment target / value type compatibility</item>
///         <item>Return expression type vs. declared return type</item>
///     </list>
///     Type inference is conservative — only literals, casts, locals, and
///     owner-type members are inferred.  Unknown types are silently skipped
///     (no false positives).
/// </summary>
public sealed class ExprChecker
{
    private readonly TranspileContext _ctx;
    private readonly TgmlFile _file;
    private readonly TgmlTypeDecl? _owner;
    private readonly string _returnType; // "void" or declared return type name

    public ExprChecker(
        TranspileContext ctx,
        TgmlFile file,
        SymbolTable symbols,
        TgmlTypeDecl? owner = null,
        string returnType = "void")
    {
        _ctx = ctx;
        _file = file;
        Symbols = symbols;
        _owner = owner;
        _returnType = returnType;
    }

    public SymbolTable Symbols { get; }

    // ── Statement walker ──────────────────────────────────────────────────────

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
                if (v.Initializer is not null)
                    CheckAssignCompatibility(v.Type.Name.Full, v.Initializer,
                        v.Line, v.Column, $"Cannot assign '{{1}}' to variable of type '{v.Type.Name.Full}'.");
                Symbols.Define(v.Name, v.Type);
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

                if (i.ElseBlock is not null) CheckBlock(i.ElseBlock);
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
                        if (d.Initializer is not null) CheckExpr(d.Initializer);
                        Symbols.Define(d.Name, d.Type);
                        break;
                    case TgmlForInitExpr e:
                        CheckExpr(e.Expression);
                        break;
                }

                if (f.Condition is not null) CheckExpr(f.Condition);
                foreach (var u in f.Updates) CheckExpr(u);
                foreach (var s in f.Body.Statements) CheckStmt(s);
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
                    if (sec.CaseValue is not null) CheckExpr(sec.CaseValue);
                    foreach (var s in sec.Statements) CheckStmt(s);
                }

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
                    foreach (var s in cc.Body.Statements) inner.CheckStmt(s);
                    inner.Symbols.PopScope();
                }

                if (tr.FinallyBlock is not null) CheckBlock(tr.FinallyBlock);
                break;
        }
    }

    // ── Return checking ───────────────────────────────────────────────────────

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
            // bare return; in non-void context — warn only (branch might be unreachable)
            _ctx.AddWarning($"Missing return value; method declares return type '{_returnType}'.",
                _file.FileName, ret.Line, ret.Column);
            return;
        }

        CheckAssignCompatibility(_returnType, ret.Value, ret.Line, ret.Column,
            $"Return type mismatch: cannot convert '{{1}}' to '{_returnType}'.");
    }

    // ── Expression type-checker ───────────────────────────────────────────────

    public void CheckExpr(TgmlExpression expr) => InferType(expr);

    /// <summary>
    ///     Returns the inferred primitive type of <paramref name="expr" />, or
    ///     <c>null</c> when the type cannot be statically determined.
    /// </summary>
    public string? InferType(TgmlExpression expr) =>
        expr switch
        {
            TgmlIntLiteralExpr => "int",
            TgmlRealLiteralExpr => "real",
            TgmlBoolLiteralExpr => "bool",
            TgmlStringLiteralExpr => "string",
            TgmlNullExpr => "null",
            TgmlParenExpr p => InferType(p.Inner),
            TgmlCastExpr c => c.TargetType.Name.Full,
            TgmlIdExpr id => InferIdType(id),
            TgmlValueKeywordExpr => InferKeywordVar("value"),
            TgmlFieldKeywordExpr => InferKeywordVar("field"),
            TgmlUnaryExpr u => InferUnary(u),
            TgmlBinaryExpr b => InferBinary(b),
            TgmlTernaryExpr t => InferTernary(t),
            TgmlAssignExpr a => InferAssign(a),
            TgmlArrayInitExpr ai => VisitArrayInit(ai),
            TgmlMethodCallExpr mc => VisitMethodCall(mc),
            TgmlFuncCallExpr fc => VisitFuncCall(fc),
            TgmlNewObjectExpr no => VisitNewObject(no),
            TgmlNewArrayExpr na => VisitNewArray(na),
            TgmlIndexExpr ix => VisitIndex(ix),
            TgmlFieldAccessExpr fa => VisitFieldAccess(fa),
            TgmlLambdaExpr lam => VisitLambda(lam),
            _ => null
        };

    // ── Inference helpers ─────────────────────────────────────────────────────

    private string? InferIdType(TgmlIdExpr id)
    {
        // 1. Local variable / parameter in scope
        if (Symbols.TryResolve(id.Name, out var typeRef) && typeRef is not null)
            return typeRef.Name.Full;

        // 2. Property on the owning type
        var prop = _owner switch
        {
            TgmlClassDecl cls => cls.Properties.FirstOrDefault(p => p.Name == id.Name),
            TgmlStructDecl str => str.Properties.FirstOrDefault(p => p.Name == id.Name),
            _ => null
        };
        if (prop is not null) return prop.Type.Name.Full;

        // 3. Field on the owning type
        var field = _owner switch
        {
            TgmlClassDecl cls => cls.Fields.FirstOrDefault(f => f.Name == id.Name),
            TgmlStructDecl str => str.Fields.FirstOrDefault(f => f.Name == id.Name),
            _ => null
        };
        return field?.Type.Name.Full;
    }

    /// <summary>
    ///     Resolves a contextual-keyword identifier (<c>value</c> / <c>field</c>) that may
    ///     also be used as a local variable name.
    /// </summary>
    private string? InferKeywordVar(string keyword)
    {
        if (Symbols.TryResolve(keyword, out var typeRef) && typeRef is not null)
            return typeRef.Name.Full;
        return null;
    }

    private string? InferUnary(TgmlUnaryExpr expr)
    {
        var t = InferType(expr.Operand);
        if (t is null) return null;

        switch (expr.Operator)
        {
            case "-":
                if (!TypeCompatibility.IsNumeric(t))
                    Error(expr, $"Operator '-' cannot be applied to type '{t}'.");
                return TypeCompatibility.IsNumeric(t) ? t : null;

            case "~":
                if (t != "int")
                    Error(expr, $"Operator '~' requires 'int', got '{t}'.");
                return "int";

            case "not":
                if (t != "bool")
                    Error(expr, $"Operator 'not' requires 'bool', got '{t}'.");
                return "bool";
        }

        return null;
    }

    private string? InferBinary(TgmlBinaryExpr expr)
    {
        var lt = InferType(expr.Left);
        var rt = InferType(expr.Right);

        if (lt is null || rt is null) return null;

        switch (expr.Operator)
        {
            case "+" or "-" or "*" or "/" or "%":
                if (expr.Operator == "+" && lt == "string" && rt == "string") return "string";
                if (!TypeCompatibility.IsNumeric(lt) || !TypeCompatibility.IsNumeric(rt))
                    Error(expr, $"Operator '{expr.Operator}' cannot be applied to '{lt}' and '{rt}'.");
                return lt == "real" || rt == "real" ? "real" : "int";

            case "&" or "|" or "^" or "<<" or ">>":
                if (lt != "int" || rt != "int")
                    Error(expr,
                        $"Bitwise operator '{expr.Operator}' requires 'int' operands, got '{lt}' and '{rt}'.");
                return "int";

            case "and" or "or" or "&&" or "||":
                if (lt != "bool" || rt != "bool")
                    Error(expr,
                        $"Logical operator '{expr.Operator}' requires 'bool' operands, got '{lt}' and '{rt}'.");
                return "bool";

            case "<" or ">" or "<=" or ">=":
                if (!TypeCompatibility.IsNumeric(lt) || !TypeCompatibility.IsNumeric(rt))
                    Error(expr,
                        $"Relational operator '{expr.Operator}' requires numeric operands, got '{lt}' and '{rt}'.");
                return "bool";

            case "==" or "!=":
                if (lt != "null" && rt != "null" && !TypeCompatibility.AreComparable(lt, rt))
                    Error(expr, $"Operator '{expr.Operator}' cannot be applied to '{lt}' and '{rt}'.");
                return "bool";
        }

        return null;
    }

    private string? InferTernary(TgmlTernaryExpr expr)
    {
        CheckExpr(expr.Condition);
        var tt = InferType(expr.ThenExpr);
        var et = InferType(expr.ElseExpr);
        return tt == et ? tt : null;
    }

    private string? InferAssign(TgmlAssignExpr expr)
    {
        CheckExpr(expr.Target);

        if (expr.Operator != "=")
        {
            // Compound: expand op= → check as binary
            var lt = InferType(expr.Target);
            var rt = InferType(expr.Value);
            if (lt is not null && rt is not null)
                InferBinary(new TgmlBinaryExpr
                    { Left = expr.Target, Operator = expr.Operator[..^1], Right = expr.Value, Line = expr.Line, Column = expr.Column });
        }
        else
        {
            var lt = InferType(expr.Target);
            var rt = InferType(expr.Value);
            if (lt is not null && rt is not null && !TypeCompatibility.AreAssignable(lt, rt))
                Error(expr, $"Cannot assign '{rt}' to '{lt}'.");
            else
                CheckExpr(expr.Value);
        }

        return InferType(expr.Target);
    }

    // ── Visitor stubs (recurse, return null) ─────────────────────────────────

    private string? VisitArrayInit(TgmlArrayInitExpr expr)
    {
        foreach (var el in expr.Elements) CheckExpr(el);
        return null;
    }

    private string? VisitMethodCall(TgmlMethodCallExpr mc)
    {
        CheckExpr(mc.Target);
        foreach (var a in mc.Args) CheckExpr(a);
        return null;
    }

    private string? VisitFuncCall(TgmlFuncCallExpr fc)
    {
        foreach (var a in fc.Args) CheckExpr(a);
        return null;
    }

    private string VisitNewObject(TgmlNewObjectExpr no)
    {
        foreach (var a in no.Args) CheckExpr(a);
        return no.Type.Name.Full;
    }

    private string? VisitNewArray(TgmlNewArrayExpr na)
    {
        CheckExpr(na.Size);
        return null;
    }

    private string? VisitIndex(TgmlIndexExpr ix)
    {
        CheckExpr(ix.Target);
        CheckExpr(ix.Index);
        return null;
    }

    private string? VisitFieldAccess(TgmlFieldAccessExpr fa)
    {
        CheckExpr(fa.Target);
        return null;
    }

    private string? VisitLambda(TgmlLambdaExpr lam)
    {
        var inner = new ExprChecker(_ctx, _file, Symbols, _owner, _returnType);
        inner.Symbols.PushScope();
        foreach (var p in lam.Params) inner.Symbols.Define(p.Name, p.Type);
        if (lam.ExprBody is not null) inner.CheckExpr(lam.ExprBody);
        else if (lam.BlockBody is not null)
            foreach (var s in lam.BlockBody.Statements)
                inner.CheckStmt(s);
        inner.Symbols.PopScope();
        return null;
    }

    // ── Utilities ─────────────────────────────────────────────────────────────

    /// <summary>Checks that <paramref name="valueExpr" />'s type is assignable to <paramref name="targetType" />.</summary>
    private void CheckAssignCompatibility(string targetType, TgmlExpression valueExpr, int line, int col,
        string messageTemplate)
    {
        // InferType already recurses into all sub-expressions and fires any operand errors.
        var inferred = InferType(valueExpr);
        if (inferred is not null && !TypeCompatibility.AreAssignable(targetType, inferred))
            Error(line, col, messageTemplate.Replace("{1}", inferred));
    }

    private void Error(TgmlExpression expr, string msg) =>
        _ctx.AddError(msg, _file.FileName, expr.Line, expr.Column);

    private void Error(int line, int col, string msg) =>
        _ctx.AddError(msg, _file.FileName, line, col);
}

