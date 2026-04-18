using Antlr4.Runtime.Misc;
using TypedGML.Transpiler.Population.Models;
using TypedGML.Transpiler.Visitor;

namespace TypedGML.Transpiler.Population;

public sealed partial class AstVisitor
{
    // ── Members ───────────────────────────────────────────────────────────────

    public override object? VisitMemberDecl([NotNull] TypedGMLParser.MemberDeclContext ctx)
        => VisitChildren(ctx);

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

    public override object? VisitAccessorDecl([NotNull] TypedGMLParser.AccessorDeclContext ctx)
    {
        var isGet = ctx.GET() is not null;
        AccessModifier? accMod = ctx.accessMod() is { } am ? AccessMod(am) : null;
        return new TgmlAccessorDecl { IsGet = isGet, AccessMod = accMod, Body = ctx.block() is { } b ? (TgmlBlock)Visit(b)! : null };
    }

    public override object? VisitMethodDecl([NotNull] TypedGMLParser.MethodDeclContext ctx)
    {
        var mods = ParseMethodModifiers(ctx.methodModifiers());
        return new TgmlMethodDecl
        {
            Name = NameId(ctx.nameId()),
            Access = mods.Access,
            Modifiers = mods,
            IsNocheck = ctx.NOCHECK() is not null,
            ReturnType = TypeRef(ctx.typeRef()),
            TypeParams = TypeParams(ctx.typeParams()),
            Params = ParamList(ctx.paramList()),
            Decorators = Decorators(ctx.decorator()),
            Body = ctx.block() is { } b ? (TgmlBlock)Visit(b)! : null
        };
    }

    public override object? VisitConstructorDecl([NotNull] TypedGMLParser.ConstructorDeclContext ctx)
    {
        List<TgmlExpression>? baseArgs = null;
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
            Body = (TgmlBlock)Visit(ctx.block())!
        };
    }

    // ── Interface members ─────────────────────────────────────────────────────

    public override object? VisitInterfaceMemberDecl([NotNull] TypedGMLParser.InterfaceMemberDeclContext ctx)
        => VisitChildren(ctx);

    public override object? VisitInterfaceMethodDecl([NotNull] TypedGMLParser.InterfaceMethodDeclContext ctx)
        => new TgmlInterfaceMethodDecl
        {
            Name = NameId(ctx.nameId()),
            ReturnType = TypeRef(ctx.typeRef()),
            TypeParams = TypeParams(ctx.typeParams()),
            Params = ParamList(ctx.paramList()),
            Decorators = Decorators(ctx.decorator()),
            Body = ctx.block() is { } b ? (TgmlBlock)Visit(b)! : null
        };

    public override object? VisitInterfacePropertyDecl([NotNull] TypedGMLParser.InterfacePropertyDeclContext ctx)
        => new TgmlInterfacePropertyDecl
        {
            Name = NameId(ctx.nameId()),
            Type = TypeRef(ctx.typeRef()),
            Decorators = Decorators(ctx.decorator()),
            Accessors = ctx.interfaceAccessorDecl()
                .Select(a => (TgmlInterfaceAccessorDecl)Visit(a)!)
                .ToList()
        };

    public override object? VisitInterfaceAccessorDecl([NotNull] TypedGMLParser.InterfaceAccessorDeclContext ctx)
        => new TgmlInterfaceAccessorDecl { IsGet = ctx.GET() is not null };

    // ── Parameters & decorators ───────────────────────────────────────────────

    public override object? VisitParam([NotNull] TypedGMLParser.ParamContext ctx)
        => new TgmlParam
        {
            Type = TypeRef(ctx.typeRef()),
            Name = NameId(ctx.nameId()),
            Decorators = Decorators(ctx.decorator()),
            Default = ctx.expression() is { } e ? (TgmlExpression)Visit(e)! : null
        };

    public override object? VisitDecorator([NotNull] TypedGMLParser.DecoratorContext ctx)
        => new TgmlDecorator { Name = QName(ctx.qualifiedName()), Args = ArgList(ctx.argList()) };
}

