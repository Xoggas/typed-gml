using Antlr4.Runtime.Misc;
using TypedGML.Transpiler.Population.Models;
using TypedGML.Transpiler.Visitor;

namespace TypedGML.Transpiler.Population;

public sealed partial class AstVisitor
{
    public override object? VisitBlock([NotNull] TypedGMLParser.BlockContext ctx)
        => new TgmlBlock
        {
            Line = Line(ctx),
            Statements = ctx.statement().Select(s => (TgmlStatement)Visit(s)!).ToList()
        };

    public override object? VisitStatement([NotNull] TypedGMLParser.StatementContext ctx)
        => VisitChildren(ctx);

    public override object? VisitLocalVarDecl([NotNull] TypedGMLParser.LocalVarDeclContext ctx)
        => new TgmlLocalVarDecl
        {
            Line = Line(ctx),
            Type = TypeRef(ctx.typeRef()),
            Name = NameId(ctx.nameId()),
            Initializer = ctx.expression() is { } e ? (TgmlExpression)Visit(e)! : null
        };

    public override object? VisitExpressionStmt([NotNull] TypedGMLParser.ExpressionStmtContext ctx)
        => new TgmlExpressionStmt { Line = Line(ctx), Expression = (TgmlExpression)Visit(ctx.expression())! };

    public override object? VisitIfStmt([NotNull] TypedGMLParser.IfStmtContext ctx)
    {
        var exprs = ctx.expression().Select(e => (TgmlExpression)Visit(e)!).ToList();
        var blocks = ctx.block().Select(b => (TgmlBlock)Visit(b)!).ToList();
        var branches = exprs.Select((e, i) => new TgmlIfBranch { Condition = e, Body = blocks[i] }).ToList();
        var elseBlock = exprs.Count < blocks.Count ? blocks[^1] : null;
        return new TgmlIfStmt { Line = Line(ctx), Branches = branches, ElseBlock = elseBlock };
    }

    public override object? VisitWhileStmt([NotNull] TypedGMLParser.WhileStmtContext ctx)
        => new TgmlWhileStmt
        {
            Line = Line(ctx),
            Condition = (TgmlExpression)Visit(ctx.expression())!,
            Body = (TgmlBlock)Visit(ctx.block())!
        };

    public override object? VisitForStmt([NotNull] TypedGMLParser.ForStmtContext ctx)
    {
        var init = ctx.forInit() is { } fi ? (TgmlForInit?)Visit(fi) : null;
        var cond = ctx.expression() is { } e ? (TgmlExpression)Visit(e)! : null;
        var updates = ctx.forUpdate() is { } fu
            ? fu.expression().Select(u => (TgmlExpression)Visit(u)!).ToList()
            : new List<TgmlExpression>();
        return new TgmlForStmt
            { Line = Line(ctx), Init = init, Condition = cond, Updates = updates, Body = (TgmlBlock)Visit(ctx.block())! };
    }

    public override object? VisitForInit([NotNull] TypedGMLParser.ForInitContext ctx)
    {
        if (ctx.typeRef() is { } tr)
            return new TgmlForInitDecl
            {
                Type = TypeRef(tr),
                Name = NameId(ctx.nameId()),
                Initializer = ctx.expression() is { } e ? (TgmlExpression)Visit(e)! : null
            };
        if (ctx.expression() is { } expr)
            return new TgmlForInitExpr { Expression = (TgmlExpression)Visit(expr)! };
        return null;
    }

    public override object? VisitRepeatStmt([NotNull] TypedGMLParser.RepeatStmtContext ctx)
        => new TgmlRepeatStmt
        {
            Line = Line(ctx),
            Count = (TgmlExpression)Visit(ctx.expression())!,
            Body = (TgmlBlock)Visit(ctx.block())!
        };

    public override object? VisitSwitchStmt([NotNull] TypedGMLParser.SwitchStmtContext ctx)
        => new TgmlSwitchStmt
        {
            Line = Line(ctx),
            Value = (TgmlExpression)Visit(ctx.expression())!,
            Sections = ctx.switchSection().Select(s => (TgmlSwitchSection)Visit(s)!).ToList()
        };

    public override object? VisitSwitchSection([NotNull] TypedGMLParser.SwitchSectionContext ctx)
    {
        TgmlExpression? caseVal = ctx.expression() is { } e ? (TgmlExpression)Visit(e)! : null;
        return new TgmlSwitchSection
        {
            CaseValue = caseVal,
            Statements = ctx.statement().Select(s => (TgmlStatement)Visit(s)!).ToList()
        };
    }

    public override object? VisitWithStmt([NotNull] TypedGMLParser.WithStmtContext ctx)
        => new TgmlWithStmt
        {
            Line = Line(ctx),
            IterType = TypeRef(ctx.typeRef()),
            VarName = NameId(ctx.nameId()),
            Body = (TgmlBlock)Visit(ctx.block())!
        };

    public override object? VisitReturnStmt([NotNull] TypedGMLParser.ReturnStmtContext ctx)
        => new TgmlReturnStmt
        {
            Line = Line(ctx),
            Value = ctx.expression() is { } e ? (TgmlExpression)Visit(e)! : null
        };

    public override object? VisitBreakStmt([NotNull] TypedGMLParser.BreakStmtContext ctx)
        => new TgmlBreakStmt { Line = Line(ctx) };

    public override object? VisitContinueStmt([NotNull] TypedGMLParser.ContinueStmtContext ctx)
        => new TgmlContinueStmt { Line = Line(ctx) };

    public override object? VisitTryStmt([NotNull] TypedGMLParser.TryStmtContext ctx)
        => new TgmlTryStmt
        {
            Line = Line(ctx),
            TryBlock = (TgmlBlock)Visit(ctx.block())!,
            CatchClauses = ctx.catchClause().Select(c => (TgmlCatchClause)Visit(c)!).ToList(),
            FinallyBlock = ctx.finallyClause() is { } fc ? (TgmlBlock)Visit(fc.block())! : null
        };

    public override object? VisitCatchClause([NotNull] TypedGMLParser.CatchClauseContext ctx)
        => new TgmlCatchClause
        {
            ExceptionType = TypeRef(ctx.typeRef()),
            VarName = NameId(ctx.nameId()),
            Body = (TgmlBlock)Visit(ctx.block())!
        };

    public override object? VisitRawStmt([NotNull] TypedGMLParser.RawStmtContext ctx)
        => new TgmlRawStmt { Line = Line(ctx), RawText = ctx.RAW_LINE().GetText() };
}

