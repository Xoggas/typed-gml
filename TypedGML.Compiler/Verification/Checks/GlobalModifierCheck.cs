using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Verification.Checks;

public sealed class GlobalModifierCheck : ISemanticCheck
{
    public bool Matches(IAstNode node) => node is FieldDeclarationNode or PropertyDeclarationNode;

    public void Check(IAstNode node, VerificationContext ctx)
    {
        if (node is FieldDeclarationNode field && field.Modifiers.Contains("global", StringComparer.Ordinal))
            ctx.Diagnostics.Report(DiagnosticCode.GlobalModifierOnField, DiagnosticSeverity.Error, "global modifier is only valid on properties.", field.Location);
    }
}
