using Antlr4.Runtime.Misc;
using TypedGML.Transpiler.Population.Models;
using TypedGML.Transpiler.Visitor;

namespace TypedGML.Transpiler.Population;

public sealed partial class AstVisitor
{
    public override object? VisitMethodCallExpr([NotNull] TypedGMLParser.MethodCallExprContext ctx)
        => new TgmlMethodCallExpr
        {
            Line = Line(ctx),
            Target = (TgmlExpression)Visit(ctx.expression())!,
            MethodName = NameId(ctx.nameId()),
            Args = ArgList(ctx.argList())
        };

    public override object? VisitFieldAccessExpr([NotNull] TypedGMLParser.FieldAccessExprContext ctx)
        => new TgmlFieldAccessExpr
        {
            Line = Line(ctx),
            Target = (TgmlExpression)Visit(ctx.expression())!,
            FieldName = NameId(ctx.nameId())
        };

    public override object? VisitIndexExpr([NotNull] TypedGMLParser.IndexExprContext ctx)
        => new TgmlIndexExpr
        {
            Line = Line(ctx),
            Target = (TgmlExpression)Visit(ctx.expression(0))!,
            Index = (TgmlExpression)Visit(ctx.expression(1))!
        };

    public override object? VisitInvokeExpr([NotNull] TypedGMLParser.InvokeExprContext ctx)
        => new TgmlInvokeExpr
        {
            Line = Line(ctx),
            Target = (TgmlExpression)Visit(ctx.expression())!,
            Args = ArgList(ctx.argList())
        };

    public override object? VisitUnaryExpr([NotNull] TypedGMLParser.UnaryExprContext ctx)
        => new TgmlUnaryExpr
        {
            Line = Line(ctx),
            Operator = ctx.GetChild(0).GetText(),
            Operand = (TgmlExpression)Visit(ctx.expression())!
        };

    public override object? VisitCastExpr([NotNull] TypedGMLParser.CastExprContext ctx)
        => new TgmlCastExpr { Line = Line(ctx), TargetType = TypeRef(ctx.typeRef()), Operand = (TgmlExpression)Visit(ctx.expression())! };

    public override object? VisitMulDivMod([NotNull] TypedGMLParser.MulDivModContext ctx) => Binary(ctx, ctx.GetChild(1).GetText());
    public override object? VisitAddSub([NotNull] TypedGMLParser.AddSubContext ctx) => Binary(ctx, ctx.GetChild(1).GetText());
    public override object? VisitLeftShiftExpr([NotNull] TypedGMLParser.LeftShiftExprContext ctx) => Binary(ctx, "<<");
    public override object? VisitRightShiftExpr([NotNull] TypedGMLParser.RightShiftExprContext ctx) => Binary(ctx, ">>");
    public override object? VisitBitwiseAnd([NotNull] TypedGMLParser.BitwiseAndContext ctx) => Binary(ctx, "&");
    public override object? VisitBitwiseXor([NotNull] TypedGMLParser.BitwiseXorContext ctx) => Binary(ctx, "^");
    public override object? VisitBitwiseOr([NotNull] TypedGMLParser.BitwiseOrContext ctx) => Binary(ctx, "|");
    public override object? VisitComparison([NotNull] TypedGMLParser.ComparisonContext ctx) => Binary(ctx, ctx.GetChild(1).GetText());
    public override object? VisitLogicalAnd([NotNull] TypedGMLParser.LogicalAndContext ctx) => Binary(ctx, "and");
    public override object? VisitLogicalOr([NotNull] TypedGMLParser.LogicalOrContext ctx) => Binary(ctx, "or");

    public override object? VisitIsExpr([NotNull] TypedGMLParser.IsExprContext ctx)
        => new TgmlIsExpr { Line = Line(ctx), Operand = (TgmlExpression)Visit(ctx.expression())!, CheckType = TypeRef(ctx.typeRef()) };

    public override object? VisitAsExpr([NotNull] TypedGMLParser.AsExprContext ctx)
        => new TgmlAsExpr { Line = Line(ctx), Operand = (TgmlExpression)Visit(ctx.expression())!, TargetType = TypeRef(ctx.typeRef()) };

    public override object? VisitTernaryExpr([NotNull] TypedGMLParser.TernaryExprContext ctx)
        => new TgmlTernaryExpr
        {
            Line = Line(ctx),
            Condition = (TgmlExpression)Visit(ctx.expression(0))!,
            ThenExpr = (TgmlExpression)Visit(ctx.expression(1))!,
            ElseExpr = (TgmlExpression)Visit(ctx.expression(2))!
        };

    public override object? VisitAssignExpr([NotNull] TypedGMLParser.AssignExprContext ctx)
        => new TgmlAssignExpr
        {
            Line = Line(ctx),
            Target = (TgmlExpression)Visit(ctx.expression(0))!,
            Operator = ctx.GetChild(1).GetText(),
            Value = (TgmlExpression)Visit(ctx.expression(1))!
        };

    private TgmlBinaryExpr Binary(Antlr4.Runtime.ParserRuleContext ctx, string op)
        => new()
        {
            Line = Line(ctx),
            Left = (TgmlExpression)Visit(((TypedGMLParser.ExpressionContext)ctx).GetRuleContext<TypedGMLParser.ExpressionContext>(0))!,
            Operator = op,
            Right = (TgmlExpression)Visit(((TypedGMLParser.ExpressionContext)ctx).GetRuleContext<TypedGMLParser.ExpressionContext>(1))!
        };
}
