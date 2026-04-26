using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Visitor;

namespace TypedGML.Compiler.Parsing;

public sealed partial class AstBuilder
{
    public override IAstNode VisitFieldDecl(TypedGMLParser.FieldDeclContext context) =>
        new FieldDeclarationNode(Text(context.nameId()), Text(context.typeRef()), Parts(context.fieldModifiers()), Decorators(context.decorator()), MaybeNode(context.expression()), Doc(context), Location(context));

    public override IAstNode VisitPropertyDecl(TypedGMLParser.PropertyDeclContext context) =>
        new PropertyDeclarationNode(Text(context.nameId()), Text(context.typeRef()), Parts(context.propertyModifiers()), Decorators(context.decorator()), Nodes<AccessorNode>(context.accessorDecl()), Doc(context), Location(context));

    public override IAstNode VisitIndexerDecl(TypedGMLParser.IndexerDeclContext context) =>
        new IndexerDeclarationNode(Text(context.typeRef()), Parts(context.propertyModifiers()), (ParameterNode)Node(context.param()), Decorators(context.decorator()), Nodes<AccessorNode>(context.accessorDecl()), Doc(context), Location(context));

    public override IAstNode VisitAccessorDecl(TypedGMLParser.AccessorDeclContext context) =>
        new AccessorNode(
            context.GET() is null ? AccessorKind.Set : AccessorKind.Get,
            Text(context.accessMod()),
            context.block() is not null ? Node(context.block()) : context.ARROW() is not null ? Node(context.expression()!) : null,
            Location(context));

    public override IAstNode VisitMethodDecl(TypedGMLParser.MethodDeclContext context) =>
        context.overloadableOperator() is not null
            ? new OperatorDeclarationNode(Text(context.overloadableOperator()), Parts(context.methodModifiers()), Parameters(context.paramList()), Text(context.typeRef()), Decorators(context.decorator()), MaybeNode(context.block()), Doc(context), Location(context))
            : context.IMPLICIT() is not null || context.EXPLICIT() is not null
                ? new ConversionOperatorNode(context.IMPLICIT() is null ? ConversionOperatorKind.Explicit : ConversionOperatorKind.Implicit, Text(context.typeRef()), context.paramList() is null ? new ParameterNode(string.Empty, string.Empty, null, [], Location(context)) : (ParameterNode)Node(context.paramList().param(0)), Parts(context.methodModifiers()), Decorators(context.decorator()), MaybeNode(context.block()), Doc(context), Location(context))
                : new MethodDeclarationNode(Text(context.nameId()), Text(context.typeRef()), Parts(context.methodModifiers()), GenericParams(context.typeParams()), Parameters(context.paramList()), Decorators(context.decorator()), MaybeNode(context.block()), Doc(context), Location(context));

    public override IAstNode VisitConstructorDecl(TypedGMLParser.ConstructorDeclContext context) =>
        new ConstructorDeclarationNode(Parts(context.accessMod()), Parameters(context.paramList()), Decorators(context.decorator()), context.BASE() is not null ? ConstructorChainTarget.Base : context.THIS() is not null ? ConstructorChainTarget.This : ConstructorChainTarget.None, Args(context.argList()).PositionalArgs.Concat<IAstNode>(Args(context.argList()).NamedArgs).ToList(), Node(context.block()), Doc(context), Location(context));

    public override IAstNode VisitStaticConstructorDecl(TypedGMLParser.StaticConstructorDeclContext context) =>
        new StaticConstructorDeclarationNode(Text(context.nameId()), Node(context.block()), Location(context));

    public override IAstNode VisitEventDecl(TypedGMLParser.EventDeclContext context) =>
        new EventDeclarationNode(Text(context.nameId()), Text(context.typeRef()), Parts(context.accessMod()), Decorators(context.decorator()), Doc(context), Location(context));

    public override IAstNode VisitDecorator(TypedGMLParser.DecoratorContext context) =>
        new DecoratorNode(Text(context.qualifiedName()), DecoratorArgs(context.argList()), Location(context));

    public override IAstNode VisitParam(TypedGMLParser.ParamContext context) =>
        new ParameterNode(Text(context.nameId()), Text(context.typeRef()), MaybeNode(context.expression()), Decorators(context.decorator()), Location(context));

    public override IAstNode VisitInterfaceMethodDecl(TypedGMLParser.InterfaceMethodDeclContext context) =>
        new MethodDeclarationNode(Text(context.nameId()), Text(context.typeRef()), [], GenericParams(context.typeParams()), Parameters(context.paramList()), Decorators(context.decorator()), MaybeNode(context.block()), Doc(context), Location(context));

    public override IAstNode VisitInterfacePropertyDecl(TypedGMLParser.InterfacePropertyDeclContext context) =>
        new PropertyDeclarationNode(Text(context.nameId()), Text(context.typeRef()), [], Decorators(context.decorator()), Nodes<AccessorNode>(context.interfaceAccessorDecl()), Doc(context), Location(context));

    public override IAstNode VisitInterfaceIndexerDecl(TypedGMLParser.InterfaceIndexerDeclContext context) =>
        new IndexerDeclarationNode(Text(context.typeRef()), [], (ParameterNode)Node(context.param()), Decorators(context.decorator()), Nodes<AccessorNode>(context.interfaceAccessorDecl()), Doc(context), Location(context));

    public override IAstNode VisitInterfaceEventDecl(TypedGMLParser.InterfaceEventDeclContext context) =>
        new EventDeclarationNode(Text(context.nameId()), Text(context.typeRef()), [], Decorators(context.decorator()), Doc(context), Location(context));

    public override IAstNode VisitInterfaceAccessorDecl(TypedGMLParser.InterfaceAccessorDeclContext context) =>
        new AccessorNode(context.GET() is null ? AccessorKind.Set : AccessorKind.Get, null, null, Location(context));
}
