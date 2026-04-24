using Antlr4.Runtime.Misc;
using TypedGML.Transpiler.Population.Models;
using TypedGML.Transpiler.Visitor;

namespace TypedGML.Transpiler.Population;

public sealed partial class AstVisitor
{
    public override object? VisitInterfaceMemberDecl([NotNull] TypedGMLParser.InterfaceMemberDeclContext ctx) => VisitChildren(ctx);

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
            Accessors = ctx.interfaceAccessorDecl().Select(a => (TgmlInterfaceAccessorDecl)Visit(a)!).ToList()
        };

    public override object? VisitInterfaceIndexerDecl([NotNull] TypedGMLParser.InterfaceIndexerDeclContext ctx)
        => new TgmlInterfacePropertyDecl
        {
            Name = NameId(ctx.nameId()),
            Type = TypeRef(ctx.typeRef()),
            IndexParam = (TgmlParam)Visit(ctx.param())!,
            Decorators = Decorators(ctx.decorator()),
            Accessors = ctx.interfaceAccessorDecl().Select(a => (TgmlInterfaceAccessorDecl)Visit(a)!).ToList()
        };

    public override object? VisitInterfaceAccessorDecl([NotNull] TypedGMLParser.InterfaceAccessorDeclContext ctx)
        => new TgmlInterfaceAccessorDecl { IsGet = ctx.GET() is not null };

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

    private TgmlMethodDecl BuildOperatorMethod(
        TypedGMLParser.MethodDeclContext ctx,
        MethodModifiers mods,
        List<TgmlDecorator> decorators,
        TgmlBlock? body,
        List<TgmlParam> parameters)
    {
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

    private static string GetOverloadableOperatorToken(TypedGMLParser.OverloadableOperatorContext ctx)
        => ctx.GT().Length == 2 ? ">>" : ctx.GetText();

    private static TgmlBlock WrapAccessorExpression(TgmlExpression expression, bool isGet, int line)
    {
        TgmlStatement statement = isGet
            ? new TgmlReturnStmt { Line = line, Value = expression }
            : new TgmlExpressionStmt { Line = line, Expression = expression };

        return new TgmlBlock { Line = line, Statements = [statement] };
    }
}
