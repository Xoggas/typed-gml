using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Declarations;
using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Verification.Checks;

public sealed class StaticModifierCheck : ISemanticCheck
{
    public bool Matches(IAstNode node) =>
        node is ClassDeclarationNode or StructDeclarationNode or DelegateDeclarationNode or FunctionDeclarationNode or
            FieldDeclarationNode or PropertyDeclarationNode or IndexerDeclarationNode or MethodDeclarationNode or
            ConstructorDeclarationNode or OperatorDeclarationNode or ConversionOperatorNode or EventDeclarationNode;

    public void Check(IAstNode node, VerificationContext ctx)
    {
        if (DecoratorHelper.IsBcl(node.Location))
            return;

        if (Modifiers(node).Contains("static", StringComparer.Ordinal))
            ctx.Diagnostics.Report(DiagnosticCode.StaticModifierNotSupported, DiagnosticSeverity.Error, "static modifier is not supported in user code.", node.Location);
    }

    private static IReadOnlyList<string> Modifiers(IAstNode node) => node switch
    {
        ClassDeclarationNode n => n.Modifiers,
        StructDeclarationNode n => n.Modifiers,
        DelegateDeclarationNode n => n.Modifiers,
        FunctionDeclarationNode n => n.Modifiers,
        FieldDeclarationNode n => n.Modifiers,
        PropertyDeclarationNode n => n.Modifiers,
        IndexerDeclarationNode n => n.Modifiers,
        MethodDeclarationNode n => n.Modifiers,
        ConstructorDeclarationNode n => n.Modifiers,
        OperatorDeclarationNode n => n.Modifiers,
        ConversionOperatorNode n => n.Modifiers,
        EventDeclarationNode n => n.Modifiers,
        _ => []
    };
}
