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
        => new TgmlCastExpr
            { Line = Line(ctx), TargetType = TypeRef(ctx.typeRef()), Operand = (TgmlExpression)Visit(ctx.expression())! };

    private TgmlBinaryExpr Binary(Antlr4.Runtime.ParserRuleContext ctx, string op)
        => new()
        {
            Line = Line(ctx),
            Left = (TgmlExpression)Visit(((TypedGMLParser.ExpressionContext)ctx).GetRuleContext<TypedGMLParser.ExpressionContext>(0))!,
            Operator = op,
            Right = (TgmlExpression)Visit(((TypedGMLParser.ExpressionContext)ctx).GetRuleContext<TypedGMLParser.ExpressionContext>(1))!
        };

    public override object? VisitMulDivMod([NotNull] TypedGMLParser.MulDivModContext ctx)
        => new TgmlBinaryExpr
        {
            Line = Line(ctx),
            Left = (TgmlExpression)Visit(ctx.expression(0))!,
            Operator = ctx.GetChild(1).GetText(),
            Right = (TgmlExpression)Visit(ctx.expression(1))!
        };

    public override object? VisitAddSub([NotNull] TypedGMLParser.AddSubContext ctx)
        => new TgmlBinaryExpr
        {
            Line = Line(ctx),
            Left = (TgmlExpression)Visit(ctx.expression(0))!,
            Operator = ctx.GetChild(1).GetText(),
            Right = (TgmlExpression)Visit(ctx.expression(1))!
        };

    public override object? VisitLeftShiftExpr([NotNull] TypedGMLParser.LeftShiftExprContext ctx)
        => new TgmlBinaryExpr
            { Line = Line(ctx), Left = (TgmlExpression)Visit(ctx.expression(0))!, Operator = "<<", Right = (TgmlExpression)Visit(ctx.expression(1))! };

    public override object? VisitRightShiftExpr([NotNull] TypedGMLParser.RightShiftExprContext ctx)
        => new TgmlBinaryExpr
            { Line = Line(ctx), Left = (TgmlExpression)Visit(ctx.expression(0))!, Operator = ">>", Right = (TgmlExpression)Visit(ctx.expression(1))! };

    public override object? VisitBitwiseAnd([NotNull] TypedGMLParser.BitwiseAndContext ctx)
        => new TgmlBinaryExpr
            { Line = Line(ctx), Left = (TgmlExpression)Visit(ctx.expression(0))!, Operator = "&", Right = (TgmlExpression)Visit(ctx.expression(1))! };

    public override object? VisitBitwiseXor([NotNull] TypedGMLParser.BitwiseXorContext ctx)
        => new TgmlBinaryExpr
            { Line = Line(ctx), Left = (TgmlExpression)Visit(ctx.expression(0))!, Operator = "^", Right = (TgmlExpression)Visit(ctx.expression(1))! };

    public override object? VisitBitwiseOr([NotNull] TypedGMLParser.BitwiseOrContext ctx)
        => new TgmlBinaryExpr
            { Line = Line(ctx), Left = (TgmlExpression)Visit(ctx.expression(0))!, Operator = "|", Right = (TgmlExpression)Visit(ctx.expression(1))! };

    public override object? VisitComparison([NotNull] TypedGMLParser.ComparisonContext ctx)
        => new TgmlBinaryExpr
        {
            Line = Line(ctx),
            Left = (TgmlExpression)Visit(ctx.expression(0))!,
            Operator = ctx.GetChild(1).GetText(),
            Right = (TgmlExpression)Visit(ctx.expression(1))!
        };

    public override object? VisitIsExpr([NotNull] TypedGMLParser.IsExprContext ctx)
        => new TgmlIsExpr
            { Line = Line(ctx), Operand = (TgmlExpression)Visit(ctx.expression())!, CheckType = TypeRef(ctx.typeRef()) };

    public override object? VisitAsExpr([NotNull] TypedGMLParser.AsExprContext ctx)
        => new TgmlAsExpr
            { Line = Line(ctx), Operand = (TgmlExpression)Visit(ctx.expression())!, TargetType = TypeRef(ctx.typeRef()) };

    public override object? VisitLogicalAnd([NotNull] TypedGMLParser.LogicalAndContext ctx)
        => new TgmlBinaryExpr
            { Line = Line(ctx), Left = (TgmlExpression)Visit(ctx.expression(0))!, Operator = "and", Right = (TgmlExpression)Visit(ctx.expression(1))! };

    public override object? VisitLogicalOr([NotNull] TypedGMLParser.LogicalOrContext ctx)
        => new TgmlBinaryExpr
            { Line = Line(ctx), Left = (TgmlExpression)Visit(ctx.expression(0))!, Operator = "or", Right = (TgmlExpression)Visit(ctx.expression(1))! };

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

    public override object? VisitNewObjectExpr([NotNull] TypedGMLParser.NewObjectExprContext ctx)
        => new TgmlNewObjectExpr { Line = Line(ctx), Type = TypeRef(ctx.typeRef()), Args = ArgList(ctx.argList()) };

    public override object? VisitNewArrayExpr([NotNull] TypedGMLParser.NewArrayExprContext ctx)
        => new TgmlNewArrayExpr
            { Line = Line(ctx), ElementType = TypeRef(ctx.typeRef()), Size = (TgmlExpression)Visit(ctx.expression())! };

    public override object? VisitNewImplicitExpr([NotNull] TypedGMLParser.NewImplicitExprContext ctx)
        => new TgmlNewImplicitExpr { Line = Line(ctx), Args = ArgList(ctx.argList()) };

    public override object? VisitTypeofExpr([NotNull] TypedGMLParser.TypeofExprContext ctx)
        => new TgmlTypeofExpr { Line = Line(ctx), Type = TypeRef(ctx.typeRef()) };

    public override object? VisitNameofExpr([NotNull] TypedGMLParser.NameofExprContext ctx)
        => new TgmlNameofExpr { Line = Line(ctx), Operand = (TgmlExpression)Visit(ctx.expression())! };

    public override object? VisitDefaultOfExpr([NotNull] TypedGMLParser.DefaultOfExprContext ctx)
        => new TgmlDefaultExpr { Line = Line(ctx), Type = TypeRef(ctx.typeRef()) };

    public override object? VisitBaseCallExpr([NotNull] TypedGMLParser.BaseCallExprContext ctx)
        => new TgmlBaseCallExpr { Line = Line(ctx), MethodName = NameId(ctx.nameId()), Args = ArgList(ctx.argList()) };

    public override object? VisitBaseAccessExpr([NotNull] TypedGMLParser.BaseAccessExprContext ctx)
        => new TgmlBaseAccessExpr { Line = Line(ctx), MemberName = NameId(ctx.nameId()) };

    public override object? VisitLambdaExprAtom([NotNull] TypedGMLParser.LambdaExprAtomContext ctx)
        => Visit(ctx.lambdaExpr());

    public override object? VisitLambdaExpr([NotNull] TypedGMLParser.LambdaExprContext ctx)
    {
        var parms = ParamList(ctx.paramList());
        if (ctx.nameId() is { } ni)
            parms = [new TgmlParam { Type = new TgmlTypeRef { Name = new TgmlQualifiedName { Parts = ["?"] } }, Name = NameId(ni) }];

        TgmlExpression? exprBody = null;
        TgmlBlock? blockBody = null;
        if (ctx.expression() is { } e)
            exprBody = (TgmlExpression)Visit(e)!;
        else if (ctx.block() is { } b)
            blockBody = (TgmlBlock)Visit(b)!;

        return new TgmlLambdaExpr { Line = Line(ctx), Params = parms, ExprBody = exprBody, BlockBody = blockBody };
    }

    public override object? VisitArrayInitExpr([NotNull] TypedGMLParser.ArrayInitExprContext ctx)
        => new TgmlArrayInitExpr
        {
            Line = Line(ctx),
            Elements = ctx.expression().Select(e => (TgmlExpression)Visit(e)!).ToList()
        };

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

