using Antlr4.Runtime.Misc;
using TypedGML.Transpiler.Population.Models;
using TypedGML.Transpiler.Visitor;

namespace TypedGML.Transpiler.Population;

public sealed partial class AstVisitor
{
    // ── Program ───────────────────────────────────────────────────────────────

    public override object? VisitProgram([NotNull] TypedGMLParser.ProgramContext ctx)
    {
        var usings = ctx.usingDecl().Select(u => (TgmlUsing)Visit(u)!).ToList();
        var namespaces = ctx.namespaceDecl().Select(n => (TgmlNamespace)Visit(n)!).ToList();
        var typeDecls = ctx.typeDecl().Select(t => (TgmlTypeDecl)Visit(t)!).ToList();
        return new TgmlFile { Usings = usings, Namespaces = namespaces, TypeDecls = typeDecls };
    }

    public override object? VisitUsingDecl([NotNull] TypedGMLParser.UsingDeclContext ctx)
        => new TgmlUsing { Name = QName(ctx.qualifiedName()) };

    public override object? VisitNamespaceDecl([NotNull] TypedGMLParser.NamespaceDeclContext ctx)
        => new TgmlNamespace { Name = QName(ctx.qualifiedName()) };

    // ── Type declarations ─────────────────────────────────────────────────────

    public override object? VisitTypeDecl([NotNull] TypedGMLParser.TypeDeclContext ctx)
        => VisitChildren(ctx);

    public override object? VisitClassDecl([NotNull] TypedGMLParser.ClassDeclContext ctx)
    {
        var members = ctx.memberDecl().Select(Visit).ToList();
        return new TgmlClassDecl
        {
            Name = ctx.ID().GetText(),
            Access = AccessMod(ctx.accessMod()),
            ClassModifier = ClassMod(ctx.classMod()),
            Decorators = Decorators(ctx.decorator()),
            TypeParams = TypeParams(ctx.typeParams()),
            BaseTypes = InheritanceList(ctx.inheritanceList()),
            Fields = members.OfType<TgmlFieldDecl>().ToList(),
            Properties = members.OfType<TgmlPropertyDecl>().ToList(),
            Methods = members.OfType<TgmlMethodDecl>().ToList(),
            Constructors = members.OfType<TgmlConstructorDecl>().ToList(),
            NestedTypes = members.OfType<TgmlTypeDecl>().ToList()
        };
    }

    public override object? VisitStructDecl([NotNull] TypedGMLParser.StructDeclContext ctx)
    {
        var members = ctx.memberDecl().Select(Visit).ToList();
        return new TgmlStructDecl
        {
            Name = ctx.ID().GetText(),
            Access = AccessMod(ctx.accessMod()),
            IsReadonly = ctx.READONLY() is not null,
            Decorators = Decorators(ctx.decorator()),
            TypeParams = TypeParams(ctx.typeParams()),
            BaseTypes = InheritanceList(ctx.inheritanceList()),
            Fields = members.OfType<TgmlFieldDecl>().ToList(),
            Properties = members.OfType<TgmlPropertyDecl>().ToList(),
            Methods = members.OfType<TgmlMethodDecl>().ToList(),
            Constructors = members.OfType<TgmlConstructorDecl>().ToList(),
            NestedTypes = members.OfType<TgmlTypeDecl>().ToList()
        };
    }

    public override object? VisitEnumDecl([NotNull] TypedGMLParser.EnumDeclContext ctx)
        => new TgmlEnumDecl
        {
            Name = ctx.ID().GetText(),
            Access = AccessMod(ctx.accessMod()),
            Decorators = Decorators(ctx.decorator()),
            Members = ctx.enumMember().Select(m => (TgmlEnumMember)Visit(m)!).ToList()
        };

    public override object? VisitEnumMember([NotNull] TypedGMLParser.EnumMemberContext ctx)
    {
        TgmlIntLiteralExpr? val = ctx.intLiteral() is { } il
            ? new TgmlIntLiteralExpr { RawValue = il.GetText(), Line = Line(ctx) }
            : null;
        return new TgmlEnumMember
            { Name = NameId(ctx.nameId()), Decorators = Decorators(ctx.decorator()), Value = val };
    }

    public override object? VisitInterfaceDecl([NotNull] TypedGMLParser.InterfaceDeclContext ctx)
    {
        var members = ctx.interfaceMemberDecl().Select(Visit).ToList();
        return new TgmlInterfaceDecl
        {
            Name = ctx.ID().GetText(),
            Access = AccessMod(ctx.accessMod()),
            Decorators = Decorators(ctx.decorator()),
            TypeParams = TypeParams(ctx.typeParams()),
            BaseInterfaces = InheritanceList(ctx.inheritanceList()),
            Methods = members.OfType<TgmlInterfaceMethodDecl>().ToList(),
            Properties = members.OfType<TgmlInterfacePropertyDecl>().ToList()
        };
    }
}

