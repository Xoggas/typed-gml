using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Verification.Checks;

public sealed class ConstExpressionCheck : ISemanticCheck
{
    public bool Matches(IAstNode node) => node is FieldDeclarationNode;

    public void Check(IAstNode node, VerificationContext ctx)
    {
        var field = (FieldDeclarationNode)node;
        if (!field.Modifiers.Contains("const", StringComparer.Ordinal))
            return;

        if (field.Initializer is null)
            Report("const field must declare an initializer.", field.Location, ctx);
        else if (!ConstantExpressionHelper.IsConstant(field.Initializer, ctx))
            Report("const field initializer must be a compile-time constant.", field.Location, ctx);
        else if (field.Initializer is IdentifierExpressionNode id && id.Name == field.Name)
            Report("const field cannot reference itself.", field.Location, ctx);
    }

    private static void Report(string message, SourceLocation location, VerificationContext ctx) =>
        ctx.Diagnostics.Report(DiagnosticCode.NonConstantConstField, DiagnosticSeverity.Error, message, location);
}
