using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking;

/// <summary>
///     Abstract base for <see cref="IAtomicCheck" /> implementations that need to
///     walk every callable body (field initializers, constructors, methods, property accessors).
///     <para>
///         Subclasses override only the virtual <c>On*</c> hooks they care about; the base
///         handles all mechanical traversal through the full AST.
///     </para>
/// </summary>
public abstract class AstBodyWalker : IAtomicCheck
{
    public abstract string Name { get; }

    public void Execute(TranspileContext context, IReadOnlyList<TgmlFile> files)
    {
        foreach (var file in files)
        foreach (var type in file.TypeDecls)
            WalkType(context, file, type);
    }

    // ── Override these hooks ──────────────────────────────────────────────────

    /// <summary>Called when entering a type declaration (before visiting any member).</summary>
    protected virtual void OnEnterType(TranspileContext ctx, TgmlFile file, TgmlTypeDecl decl) { }

    /// <summary>Called after all members of a type have been visited.</summary>
    protected virtual void OnLeaveType(TranspileContext ctx, TgmlFile file, TgmlTypeDecl decl) { }

    /// <summary>Called when entering a callable scope (constructor, method, or property accessor).</summary>
    protected virtual void OnEnterCallable(TranspileContext ctx, TgmlFile file, WalkContext wctx) { }

    /// <summary>Called when leaving a callable scope.</summary>
    protected virtual void OnLeaveCallable(TranspileContext ctx, TgmlFile file, WalkContext wctx) { }

    /// <summary>Called when entering a block, before visiting its statements.</summary>
    protected virtual void OnEnterBlock(TranspileContext ctx, TgmlFile file, TgmlBlock block, WalkContext wctx) { }

    /// <summary>Called after all statements in a block have been visited.</summary>
    protected virtual void OnLeaveBlock(TranspileContext ctx, TgmlFile file, TgmlBlock block, WalkContext wctx) { }

    /// <summary>
    ///     Called for each statement <em>before</em> the walker recurses into its children.
    ///     Return <c>false</c> to suppress child traversal for this statement.
    /// </summary>
    protected virtual bool OnStatement(TranspileContext ctx, TgmlFile file, TgmlStatement stmt, WalkContext wctx)
        => true;

    /// <summary>Called for each statement <em>after</em> the walker has recursed into its children.</summary>
    protected virtual void AfterStatement(TranspileContext ctx, TgmlFile file, TgmlStatement stmt,
        WalkContext wctx) { }

    /// <summary>Called for each expression, <em>before</em> the walker recurses into its children.</summary>
    protected virtual void OnExpression(TranspileContext ctx, TgmlFile file, TgmlExpression expr,
        WalkContext wctx) { }

    // ── Core traversal ────────────────────────────────────────────────────────

    private void WalkType(TranspileContext ctx, TgmlFile file, TgmlTypeDecl decl)
    {
        OnEnterType(ctx, file, decl);

        switch (decl)
        {
            case TgmlClassDecl cls:
                WalkClassMembers(ctx, file, cls);
                foreach (var nested in cls.NestedTypes) WalkType(ctx, file, nested);
                break;

            case TgmlStructDecl str:
                WalkStructMembers(ctx, file, str);
                foreach (var nested in str.NestedTypes) WalkType(ctx, file, nested);
                break;

            case TgmlInterfaceDecl iface:
                foreach (var m in iface.Methods.Where(m => m.Body is not null))
                {
                    var wctx = new WalkContext
                        { OwnerType = iface, ReturnTypeName = DefaultExpressionFacts.DescribeType(m.ReturnType), Params = m.Params };
                    OnEnterCallable(ctx, file, wctx);
                    WalkBlock(ctx, file, m.Body!, wctx);
                    OnLeaveCallable(ctx, file, wctx);
                }

                break;
        }

        OnLeaveType(ctx, file, decl);
    }

    private void WalkClassMembers(TranspileContext ctx, TgmlFile file, TgmlClassDecl cls)
    {
        foreach (var field in cls.Fields)
            if (field.Initializer is not null)
                WalkExpr(ctx, file, field.Initializer, new WalkContext { OwnerType = cls, Member = field });

        if (cls.Constructor is not null)
        {
            var wctx = new WalkContext
            {
                OwnerType = cls, Member = cls.Constructor,
                InConstructor = true, Params = cls.Constructor.Params
            };
            OnEnterCallable(ctx, file, wctx);
            foreach (var arg in cls.Constructor.BaseArgs ?? []) WalkExpr(ctx, file, arg.Value, wctx);
            WalkBlock(ctx, file, cls.Constructor.Body, wctx);
            OnLeaveCallable(ctx, file, wctx);
        }

        foreach (var method in cls.Methods)
        {
            var wctx = new WalkContext
            {
                OwnerType = cls, Member = method,
                ReturnTypeName = DefaultExpressionFacts.DescribeType(method.ReturnType), Params = method.Params
            };
            OnEnterCallable(ctx, file, wctx);
            foreach (var p in method.Params)
                if (p.Default is not null) WalkExpr(ctx, file, p.Default, wctx);
            if (method.Body is not null) WalkBlock(ctx, file, method.Body, wctx);
            OnLeaveCallable(ctx, file, wctx);
        }

        foreach (var prop in cls.Properties) WalkProperty(ctx, file, cls, prop);
    }

    private void WalkStructMembers(TranspileContext ctx, TgmlFile file, TgmlStructDecl str)
    {
        foreach (var field in str.Fields)
            if (field.Initializer is not null)
                WalkExpr(ctx, file, field.Initializer, new WalkContext { OwnerType = str, Member = field });

        if (str.Constructor is not null)
        {
            var wctx = new WalkContext
            {
                OwnerType = str, Member = str.Constructor,
                InConstructor = true, Params = str.Constructor.Params
            };
            OnEnterCallable(ctx, file, wctx);
            WalkBlock(ctx, file, str.Constructor.Body, wctx);
            OnLeaveCallable(ctx, file, wctx);
        }

        foreach (var method in str.Methods)
        {
            var wctx = new WalkContext
            {
                OwnerType = str, Member = method,
                ReturnTypeName = DefaultExpressionFacts.DescribeType(method.ReturnType), Params = method.Params
            };
            OnEnterCallable(ctx, file, wctx);
            if (method.Body is not null) WalkBlock(ctx, file, method.Body, wctx);
            OnLeaveCallable(ctx, file, wctx);
        }

        foreach (var prop in str.Properties) WalkProperty(ctx, file, str, prop);
    }

    private void WalkProperty(TranspileContext ctx, TgmlFile file, TgmlTypeDecl owner, TgmlPropertyDecl prop)
    {
        if (prop.Getter?.Body is { } getterBody)
        {
            var getterParams = prop.IndexParam is not null ? [prop.IndexParam] : Array.Empty<TgmlParam>();
            var wctx = new WalkContext { OwnerType = owner, Member = prop, ReturnTypeName = DefaultExpressionFacts.DescribeType(prop.Type), Params = getterParams };
            OnEnterCallable(ctx, file, wctx);
            WalkBlock(ctx, file, getterBody, wctx);
            OnLeaveCallable(ctx, file, wctx);
        }

        if (prop.Setter?.Body is { } setterBody)
        {
            var setterParams = new List<TgmlParam>();
            if (prop.IndexParam is not null)
                setterParams.Add(prop.IndexParam);
            setterParams.Add(new TgmlParam { Name = "value", Type = prop.Type });
            var wctx = new WalkContext { OwnerType = owner, Member = prop, Params = setterParams };
            OnEnterCallable(ctx, file, wctx);
            WalkBlock(ctx, file, setterBody, wctx);
            OnLeaveCallable(ctx, file, wctx);
        }
    }

    protected void WalkBlock(TranspileContext ctx, TgmlFile file, TgmlBlock block, WalkContext wctx)
    {
        OnEnterBlock(ctx, file, block, wctx);
        foreach (var stmt in block.Statements)
            WalkStmt(ctx, file, stmt, wctx);
        OnLeaveBlock(ctx, file, block, wctx);
    }

    protected void WalkStmt(TranspileContext ctx, TgmlFile file, TgmlStatement stmt, WalkContext wctx)
    {
        if (!OnStatement(ctx, file, stmt, wctx)) return;

        switch (stmt)
        {
            case TgmlBlock b:
                WalkBlock(ctx, file, b, wctx);
                break;
            case TgmlLocalVarDecl v:
                if (v.Initializer is not null) WalkExpr(ctx, file, v.Initializer, wctx);
                break;
            case TgmlExpressionStmt es:
                WalkExpr(ctx, file, es.Expression, wctx);
                break;
            case TgmlIfStmt i:
                foreach (var br in i.Branches)
                {
                    WalkExpr(ctx, file, br.Condition, wctx);
                    WalkBlock(ctx, file, br.Body, wctx);
                }

                if (i.ElseBlock is not null) WalkBlock(ctx, file, i.ElseBlock, wctx);
                break;
            case TgmlWhileStmt w:
                WalkExpr(ctx, file, w.Condition, wctx);
                WalkBlock(ctx, file, w.Body, wctx);
                break;
            case TgmlForStmt f:
                if (f.Init is TgmlForInitDecl fd && fd.Initializer is not null)
                    WalkExpr(ctx, file, fd.Initializer, wctx);
                else if (f.Init is TgmlForInitExpr fe) WalkExpr(ctx, file, fe.Expression, wctx);
                if (f.Condition is not null) WalkExpr(ctx, file, f.Condition, wctx);
                foreach (var u in f.Updates) WalkExpr(ctx, file, u, wctx);
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
                    if (sec.CaseValue is not null) WalkExpr(ctx, file, sec.CaseValue, wctx);
                    foreach (var s in sec.Statements) WalkStmt(ctx, file, s, wctx);
                }

                break;
            case TgmlWithStmt withStmt:
                WalkBlock(ctx, file, withStmt.Body, wctx);
                break;
            case TgmlReturnStmt ret:
                if (ret.Value is not null) WalkExpr(ctx, file, ret.Value, wctx);
                break;
            case TgmlTryStmt tr:
                WalkBlock(ctx, file, tr.TryBlock, wctx);
                foreach (var cc in tr.CatchClauses) WalkBlock(ctx, file, cc.Body, wctx);
                if (tr.FinallyBlock is not null) WalkBlock(ctx, file, tr.FinallyBlock, wctx);
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
                foreach (var a in mc.Args) WalkExpr(ctx, file, a.Value, wctx);
                break;
            case TgmlFuncCallExpr fc:
                foreach (var a in fc.Args) WalkExpr(ctx, file, a.Value, wctx);
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
                foreach (var a in invoke.Args) WalkExpr(ctx, file, a.Value, wctx);
                break;
            case TgmlParenExpr p:
                WalkExpr(ctx, file, p.Inner, wctx);
                break;
            case TgmlCastExpr c:
                WalkExpr(ctx, file, c.Operand, wctx);
                break;
            case TgmlNewObjectExpr n:
                foreach (var a in n.Args) WalkExpr(ctx, file, a.Value, wctx);
                break;
            case TgmlBaseCallExpr bc:
                foreach (var a in bc.Args) WalkExpr(ctx, file, a.Value, wctx);
                break;
            case TgmlDefaultExpr:
                break;
            case TgmlNewArrayExpr na:
                WalkExpr(ctx, file, na.Size, wctx);
                break;
            case TgmlArrayInitExpr ai:
                foreach (var e in ai.Elements) WalkExpr(ctx, file, e, wctx);
                break;
            case TgmlLambdaExpr lam:
                if (lam.ExprBody is not null) WalkExpr(ctx, file, lam.ExprBody, wctx);
                else if (lam.BlockBody is not null) WalkBlock(ctx, file, lam.BlockBody, wctx);
                break;
        }
    }
}

