using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using TypedGML.Transpiler.Population.Models;
using TypedGML.Transpiler.Visitor;

namespace TypedGML.Transpiler.Population;

public sealed partial class AstVisitor : TypedGMLBaseVisitor<object?>
{
    private TgmlQualifiedName QName(TypedGMLParser.QualifiedNameContext? ctx)
    {
        if (ctx is null)
            return new TgmlQualifiedName { Parts = ["?"] };

        return new TgmlQualifiedName { Parts = ctx.ID().Select(t => t.GetText()).ToList() };
    }

    private static string NameId(TypedGMLParser.NameIdContext? ctx) => ctx?.GetText() ?? string.Empty;

    private TgmlTypeRef TypeRef(TypedGMLParser.TypeRefContext? ctx)
    {
        if (ctx is null)
            return new TgmlTypeRef { Name = new TgmlQualifiedName { Parts = ["?"] } };

        return new TgmlTypeRef
        {
            Name = QName(ctx.qualifiedName()),
            TypeArgs = ctx.typeArgs() is { } ta ? TypeArgsList(ta) : new List<TgmlTypeRef>(),
            ArrayDepth = ctx.LBRACKET().Length
        };
    }

    private List<TgmlTypeRef> TypeArgsList(TypedGMLParser.TypeArgsContext ctx)
        => ctx.typeRef().Select(TypeRef).ToList();

    private List<TgmlTypeRef> InheritanceList(TypedGMLParser.InheritanceListContext? ctx)
        => ctx is null ? [] : ctx.typeRef().Select(TypeRef).ToList();

    private List<TgmlTypeParam> TypeParams(TypedGMLParser.TypeParamsContext? ctx)
    {
        if (ctx is null)
            return [];

        return ctx.typeParam().Select(tp => new TgmlTypeParam
        {
            Name = tp.ID().GetText(),
            Constraint = tp.typeRef() is { } tr ? TypeRef(tr) : null
        }).ToList();
    }

    private List<TgmlDecorator> Decorators(TypedGMLParser.DecoratorContext[] ctxs)
        => ctxs.Select(d => (TgmlDecorator)Visit(d)!).ToList();

    private List<TgmlParam> ParamList(TypedGMLParser.ParamListContext? ctx)
        => ctx is null ? [] : ctx.param().Select(p => (TgmlParam)Visit(p)!).ToList();

    private List<TgmlArgument> ArgList(TypedGMLParser.ArgListContext? ctx)
        => ctx is null ? [] : ctx.arg().Select(a => (TgmlArgument)Visit(a)!).ToList();

    private static int Line(ParserRuleContext ctx) => ctx.Start.Line;
    private static int Column(ParserRuleContext ctx) => ctx.Start.Column;

    public override object? VisitQualifiedName([NotNull] TypedGMLParser.QualifiedNameContext ctx) => QName(ctx);
    public override object? VisitTypeRef([NotNull] TypedGMLParser.TypeRefContext ctx) => TypeRef(ctx);
}
