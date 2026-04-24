using Antlr4.Runtime.Misc;
using TypedGML.Transpiler.Population.Models;
using TypedGML.Transpiler.Visitor;

namespace TypedGML.Transpiler.Population;

public sealed partial class AstVisitor
{
    public override object? VisitNewObjectExpr([NotNull] TypedGMLParser.NewObjectExprContext ctx)
        => new TgmlNewObjectExpr { Line = Line(ctx), Type = TypeRef(ctx.typeRef()), Args = ArgList(ctx.argList()) };

    public override object? VisitNewArrayExpr([NotNull] TypedGMLParser.NewArrayExprContext ctx)
        => new TgmlNewArrayExpr { Line = Line(ctx), ElementType = TypeRef(ctx.typeRef()), Size = (TgmlExpression)Visit(ctx.expression())! };

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

    public override object? VisitLambdaExprAtom([NotNull] TypedGMLParser.LambdaExprAtomContext ctx) => Visit(ctx.lambdaExpr());

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

    public override object? VisitDictInitExpr([NotNull] TypedGMLParser.DictInitExprContext ctx)
        => new TgmlDictionaryInitExpr
        {
            Line = Line(ctx),
            Entries = ctx.dictionaryEntry().Select(e => (TgmlDictionaryEntry)Visit(e)!).ToList()
        };

    public override object? VisitDictionaryEntry([NotNull] TypedGMLParser.DictionaryEntryContext ctx)
        => new TgmlDictionaryEntry
        {
            Key = (TgmlExpression)Visit(ctx.expression(0))!,
            Value = (TgmlExpression)Visit(ctx.expression(1))!
        };
}
