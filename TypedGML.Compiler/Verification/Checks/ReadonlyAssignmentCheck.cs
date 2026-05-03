using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Verification.Checks;

public sealed class ReadonlyAssignmentCheck : ISemanticCheck
{
    public bool Matches(IAstNode node) => node is AssignmentExpressionNode;

    public void Check(IAstNode node, VerificationContext ctx)
    {
        var assignment = (AssignmentExpressionNode)node;
        var name = assignment.Target switch
        {
            IdentifierExpressionNode id => id.Name,
            MemberAccessExpressionNode access => access.MemberName,
            _ => string.Empty
        };

        var member = MemberSignatureHelper.Members(ctx.CurrentType, name, Symbols.MemberKind.Field).FirstOrDefault();
        if (member?.Modifiers.Contains("readonly", StringComparer.Ordinal) == true && !ctx.IsInConstructor)
            ctx.Diagnostics.Report(DiagnosticCode.ReadonlyFieldAssignmentOutsideConstructor, DiagnosticSeverity.Error, $"readonly field '{name}' can only be assigned in a constructor.", assignment.Location);
    }
}
