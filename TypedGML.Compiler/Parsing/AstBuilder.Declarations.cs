using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Declarations;
using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Visitor;

namespace TypedGML.Compiler.Parsing;

public sealed partial class AstBuilder
{
    public override IAstNode VisitUsingDecl(TypedGMLParser.UsingDeclContext context) =>
        new UsingDirectiveNode(Text(context.qualifiedName()), context.ID()?.GetText(), Doc(context), Location(context));

    public override IAstNode VisitNamespaceDecl(TypedGMLParser.NamespaceDeclContext context) =>
        new NamespaceDeclarationNode(Text(context.qualifiedName()), Nodes<IAstNode>(context.topLevelDecl()), context.SEMI() is not null, Doc(context), Location(context));

    public override IAstNode VisitClassDecl(TypedGMLParser.ClassDeclContext context) =>
        new ClassDeclarationNode(context.ID().GetText(), Parts(context.accessMod(), context.classMod()), GenericParams(context.typeParams()), Texts(context.inheritanceList()?.typeRef() ?? []), Decorators(context.decorator()), Nodes<IAstNode>(context.memberDecl()), Doc(context), Location(context));

    public override IAstNode VisitStructDecl(TypedGMLParser.StructDeclContext context) =>
        new StructDeclarationNode(context.ID().GetText(), Parts(context.accessMod()).Concat(context.READONLY() is null ? [] : [context.READONLY().GetText()]).ToList(), GenericParams(context.typeParams()), Texts(context.inheritanceList()?.typeRef() ?? []), Decorators(context.decorator()), Nodes<IAstNode>(context.memberDecl()), Doc(context), Location(context));

    public override IAstNode VisitInterfaceDecl(TypedGMLParser.InterfaceDeclContext context) =>
        new InterfaceDeclarationNode(context.ID().GetText(), GenericParams(context.typeParams()), Texts(context.inheritanceList()?.typeRef() ?? []), Decorators(context.decorator()), Nodes<IAstNode>(context.interfaceMemberDecl()), Doc(context), Location(context));

    public override IAstNode VisitEnumDecl(TypedGMLParser.EnumDeclContext context) =>
        new EnumDeclarationNode(context.ID().GetText(), Decorators(context.decorator()), Nodes<EnumMemberNode>(context.enumMember()), Doc(context), Location(context));

    public override IAstNode VisitEnumMember(TypedGMLParser.EnumMemberContext context) =>
        new EnumMemberNode(Text(context.nameId()), Decorators(context.decorator()), MaybeNode(context.intLiteral()), Doc(context), Location(context));

    public override IAstNode VisitDelegateDecl(TypedGMLParser.DelegateDeclContext context) =>
        new DelegateDeclarationNode(context.ID().GetText(), Parts(context.accessMod()), GenericParams(context.typeParams()), Text(context.typeRef()), Parameters(context.paramList()), Decorators(context.decorator()), Doc(context), Location(context));

    public override IAstNode VisitFunctionDecl(TypedGMLParser.FunctionDeclContext context) =>
        new FunctionDeclarationNode(Text(context.nameId()), Parts(context.methodModifiers()), GenericParams(context.typeParams()), Text(context.typeRef()), Parameters(context.paramList()), Decorators(context.decorator()), Node(context.block()), Doc(context), Location(context));

    public override IAstNode VisitTypeParam(TypedGMLParser.TypeParamContext context) =>
        new GenericParamNode(context.ID().GetText(), Text(context.typeRef()), Location(context));
}
