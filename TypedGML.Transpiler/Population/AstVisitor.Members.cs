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
        AccessModifier? accMod = ctx.accessMod() is { } am ? AccessMod(am) : null;
        var body = ctx.block() is { } b
            ? (TgmlBlock)Visit(b)!
            : ctx.expression() is { } expr
                ? WrapAccessorExpression((TgmlExpression)Visit(expr)!, isGet, Line(ctx))
                : null;
        return new TgmlAccessorDecl { IsGet = isGet, AccessMod = accMod, Body = body };
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

        if (ctx.overloadableOperator() is { } overloadableOperator)
        {
            var operatorToken = GetOverloadableOperatorToken(overloadableOperator);
            return new TgmlMethodDecl
            {
                Name = $"operator {operatorToken}",
                Access = mods.Access,
                Modifiers = mods,
                ReturnType = TypeRef(ctx.typeRef()),
                Params = parameters,
                Decorators = decorators,
                Body = body,
                OperatorToken = operatorToken
            };
        }

        var conversion = ctx.IMPLICIT() is not null ? ConversionModifier.Implicit : ConversionModifier.Explicit;
        var targetType = TypeRef(ctx.typeRef());
        return new TgmlMethodDecl
        {
            Name = $"operator {(conversion == ConversionModifier.Implicit ? "implicit" : "explicit")} {targetType}",
            Access = mods.Access,
            Modifiers = mods,
            ReturnType = targetType,
            Params = parameters,
            Decorators = decorators,
            Body = body,
            Conversion = conversion
        };
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

    private static string GetOverloadableOperatorToken(TypedGMLParser.OverloadableOperatorContext ctx)
    {
        if (ctx.GT().Length == 2)
            return ">>";

        return ctx.GetText();
    }

    private static TgmlBlock WrapAccessorExpression(TgmlExpression expression, bool isGet, int line)
    {
        TgmlStatement statement = isGet
            ? new TgmlReturnStmt { Line = line, Value = expression }
            : new TgmlExpressionStmt { Line = line, Expression = expression };

        return new TgmlBlock
        {
            Line = line,
            Statements = [statement]
        };
    }
}

