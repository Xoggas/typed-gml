using Antlr4.Runtime.Misc;
using TypedGML.Transpiler.Population.Models;
using TypedGML.Transpiler.Visitor;

namespace TypedGML.Transpiler.Population;

public sealed partial class AstVisitor
{
    public override object? VisitMemberDecl([NotNull] TypedGMLParser.MemberDeclContext ctx) => VisitChildren(ctx);

    public override object? VisitFieldDecl([NotNull] TypedGMLParser.FieldDeclContext ctx)
    {
        var mods = ParseFieldModifiers(ctx.fieldModifiers());
        return new TgmlFieldDecl
        {
            Name = NameId(ctx.nameId()),
            Access = mods.Access,
            IsStatic = mods.IsStatic,
            IsReadonly = mods.IsReadonly,
            IsConst = mods.IsConst,
            Type = TypeRef(ctx.typeRef()),
            Decorators = Decorators(ctx.decorator()),
            Initializer = ctx.expression() is { } e ? (TgmlExpression)Visit(e)! : null
        };
    }

    public override object? VisitPropertyDecl([NotNull] TypedGMLParser.PropertyDeclContext ctx)
    {
        var mods = ParsePropertyModifiers(ctx.propertyModifiers());
        return new TgmlPropertyDecl
        {
            Name = NameId(ctx.nameId()),
            Access = mods.Access,
            Modifiers = mods,
            Type = TypeRef(ctx.typeRef()),
            Decorators = Decorators(ctx.decorator()),
            Accessors = ctx.accessorDecl().Select(a => (TgmlAccessorDecl)Visit(a)!).ToList()
        };
    }

    public override object? VisitIndexerDecl([NotNull] TypedGMLParser.IndexerDeclContext ctx)
    {
        var mods = ParsePropertyModifiers(ctx.propertyModifiers());
        return new TgmlPropertyDecl
        {
            Name = NameId(ctx.nameId()),
            Access = mods.Access,
            Modifiers = mods,
            Type = TypeRef(ctx.typeRef()),
            IndexParam = (TgmlParam)Visit(ctx.param())!,
            Decorators = Decorators(ctx.decorator()),
            Accessors = ctx.accessorDecl().Select(a => (TgmlAccessorDecl)Visit(a)!).ToList()
        };
    }

    public override object? VisitAccessorDecl([NotNull] TypedGMLParser.AccessorDeclContext ctx)
    {
        var isGet = ctx.GET() is not null;
        var accessMod = ctx.accessMod() is { } am ? (AccessModifier?)AccessMod(am) : null;
        var body = ctx.block() is { } b
            ? (TgmlBlock)Visit(b)!
            : ctx.expression() is { } expr
                ? WrapAccessorExpression((TgmlExpression)Visit(expr)!, isGet, Line(ctx))
                : null;

        return new TgmlAccessorDecl { IsGet = isGet, AccessMod = accessMod, Body = body };
    }

    public override object? VisitMethodDecl([NotNull] TypedGMLParser.MethodDeclContext ctx)
    {
        var mods = ParseMethodModifiers(ctx.methodModifiers());
        var decorators = Decorators(ctx.decorator());
        var body = ctx.block() is { } b ? (TgmlBlock)Visit(b)! : null;
        var parameters = ParamList(ctx.paramList());

        if (ctx.OPERATOR() is null)
        {
            return new TgmlMethodDecl
            {
                Name = NameId(ctx.nameId()),
                Access = mods.Access,
                Modifiers = mods,
                ReturnType = TypeRef(ctx.typeRef()),
                TypeParams = TypeParams(ctx.typeParams()),
                Params = parameters,
                Decorators = decorators,
                Body = body
            };
        }

        return BuildOperatorMethod(ctx, mods, decorators, body, parameters);
    }

    public override object? VisitConstructorDecl([NotNull] TypedGMLParser.ConstructorDeclContext ctx)
    {
        List<TgmlArgument>? baseArgs = null;
        if (ctx.argList() is { } al)
            baseArgs = ArgList(al);
        else if (ctx.BASE() is not null)
            baseArgs = [];

        return new TgmlConstructorDecl
        {
            Name = "constructor",
            Access = AccessMod(ctx.accessMod()),
            Decorators = Decorators(ctx.decorator()),
            Params = ParamList(ctx.paramList()),
            BaseArgs = baseArgs,
            Body = (TgmlBlock)Visit(ctx.block())!,
            Line = ctx.Start.Line,
            Column = ctx.Start.Column
        };
    }
}
