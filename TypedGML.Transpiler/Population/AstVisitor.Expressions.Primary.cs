using Antlr4.Runtime.Misc;
using TypedGML.Transpiler.Population.Models;
using TypedGML.Transpiler.Visitor;

namespace TypedGML.Transpiler.Population;

public sealed partial class AstVisitor
{
    public override object? VisitFuncCallExpr([NotNull] TypedGMLParser.FuncCallExprContext ctx)
        => new TgmlFuncCallExpr { Line = Line(ctx), FunctionName = NameId(ctx.nameId()), Args = ArgList(ctx.argList()) };

    public override object? VisitArg([NotNull] TypedGMLParser.ArgContext ctx)
        => new TgmlArgument
        {
            Name = ctx.nameId() is { } nameId ? NameId(nameId) : null,
            Value = (TgmlExpression)Visit(ctx.expression())!
        };

    public override object? VisitParenExpr([NotNull] TypedGMLParser.ParenExprContext ctx)
        => new TgmlParenExpr { Line = Line(ctx), Inner = (TgmlExpression)Visit(ctx.expression())! };

    public override object? VisitFieldKeywordExpr([NotNull] TypedGMLParser.FieldKeywordExprContext ctx)
        => new TgmlFieldKeywordExpr { Line = Line(ctx) };

    public override object? VisitValueKeywordExpr([NotNull] TypedGMLParser.ValueKeywordExprContext ctx)
        => new TgmlValueKeywordExpr { Line = Line(ctx) };

    public override object? VisitIdExpr([NotNull] TypedGMLParser.IdExprContext ctx)
        => new TgmlIdExpr { Line = Line(ctx), Name = ctx.ID().GetText() };

    public override object? VisitNullExpr([NotNull] TypedGMLParser.NullExprContext ctx)
        => new TgmlNullExpr { Line = Line(ctx) };

    public override object? VisitDefaultExpr([NotNull] TypedGMLParser.DefaultExprContext ctx)
        => new TgmlDefaultExpr { Line = Line(ctx) };

    public override object? VisitBoolExpr([NotNull] TypedGMLParser.BoolExprContext ctx)
        => new TgmlBoolLiteralExpr { Line = Line(ctx), Value = ctx.boolLiteral().TRUE() is not null };

    public override object? VisitRealExpr([NotNull] TypedGMLParser.RealExprContext ctx)
        => new TgmlRealLiteralExpr { Line = Line(ctx), RawValue = ctx.realLiteral().GetText() };

    public override object? VisitIntExpr([NotNull] TypedGMLParser.IntExprContext ctx)
        => new TgmlIntLiteralExpr { Line = Line(ctx), RawValue = ctx.intLiteral().GetText() };

    public override object? VisitStringExpr([NotNull] TypedGMLParser.StringExprContext ctx)
        => new TgmlStringLiteralExpr { Line = Line(ctx), RawValue = ctx.stringLiteral().GetText() };
}
