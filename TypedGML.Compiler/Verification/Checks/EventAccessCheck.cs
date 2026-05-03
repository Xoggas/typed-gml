using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Diagnostics;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Verification.Checks;

public sealed class EventAccessCheck : ISemanticCheck
{
    public bool Matches(IAstNode node) => node is AssignmentExpressionNode;

    public void Check(IAstNode node, VerificationContext ctx)
    {
        var assignment = (AssignmentExpressionNode)node;
        if (assignment.Op != "=")
            return;

        var (name, owner) = assignment.Target switch
        {
            IdentifierExpressionNode id => (id.Name, ctx.CurrentType),
            MemberAccessExpressionNode access when SymbolResolver.TryResolveType(ExpressionTypeResolver.Resolve(access.Target, ctx), ctx, out var type) => (access.MemberName, type),
            _ => (string.Empty, null as TypeSymbol)
        };

        var evt = owner is null ? null : MemberSignatureHelper.Members(owner, name, MemberKind.Event).FirstOrDefault();
        if (evt is not null && owner != ctx.CurrentType)
            ctx.Diagnostics.Report(DiagnosticCode.InvalidExternalEventAssignment, DiagnosticSeverity.Error, $"Event '{name}' can only be assigned directly inside '{owner?.QualifiedName}'.", assignment.Location);
    }
}
