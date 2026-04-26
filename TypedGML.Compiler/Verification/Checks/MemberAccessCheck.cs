using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Verification.Checks;

public sealed class MemberAccessCheck : ISemanticCheck
{
    public bool Matches(IAstNode node) => node is MemberAccessExpressionNode or IdentifierExpressionNode;

    public void Check(IAstNode node, VerificationContext ctx)
    {
        switch (node)
        {
            case MemberAccessExpressionNode access:
                CheckMemberAccess(access, ctx);
                break;
            case IdentifierExpressionNode identifier:
                CheckIdentifier(identifier, ctx);
                break;
        }
    }

    private static void CheckMemberAccess(MemberAccessExpressionNode access, VerificationContext ctx)
    {
        if (!SymbolResolver.TryResolveType(ExpressionTypeResolver.Resolve(access.Target, ctx), ctx, out var targetType))
            return;

        var member = SymbolResolver.FindMember(targetType, access.MemberName, out var owner);
        if (member is null || owner is null)
        {
            Report(DiagnosticCode.TypeMismatch, $"Type '{targetType.QualifiedName}' does not contain member '{access.MemberName}'.", access.Location, ctx);
            return;
        }

        if (member.Modifiers.Contains("private", StringComparer.Ordinal) && ctx.CurrentType != owner)
            Report(DiagnosticCode.AccessViolation, $"Member '{access.MemberName}' is private to '{owner.QualifiedName}'.", access.Location, ctx);

        if (member.Modifiers.Contains("protected", StringComparer.Ordinal) && !SymbolResolver.IsWithinInheritance(ctx.CurrentType, owner))
            Report(DiagnosticCode.AccessViolation, $"Member '{access.MemberName}' is protected on '{owner.QualifiedName}'.", access.Location, ctx);
    }

    private static void CheckIdentifier(IdentifierExpressionNode identifier, VerificationContext ctx)
    {
        if (identifier.Name == "this")
        {
            if (ctx.CurrentType is null || ctx.CurrentMember?.Modifiers.Contains("static", StringComparer.Ordinal) == true)
                Report(DiagnosticCode.AccessViolation, "'this' is not available in the current context.", identifier.Location, ctx);
            return;
        }

        if (ctx.Scope.TryResolve(identifier.Name, out _))
            return;

        var member = SymbolResolver.FindMember(ctx.CurrentType, identifier.Name, out var owner);
        if (member is not null && owner is not null)
        {
            if (member.Modifiers.Contains("private", StringComparer.Ordinal) && ctx.CurrentType != owner)
                Report(DiagnosticCode.AccessViolation, $"Member '{identifier.Name}' is private to '{owner.QualifiedName}'.", identifier.Location, ctx);

            if (member.Modifiers.Contains("protected", StringComparer.Ordinal) && !SymbolResolver.IsWithinInheritance(ctx.CurrentType, owner))
                Report(DiagnosticCode.AccessViolation, $"Member '{identifier.Name}' is protected on '{owner.QualifiedName}'.", identifier.Location, ctx);

            return;
        }

        if (SymbolResolver.TryResolveType(identifier.Name, ctx, out _))
            return;

        Report(DiagnosticCode.TypeMismatch, $"Identifier '{identifier.Name}' is not declared in the current scope.", identifier.Location, ctx);
    }

    private static void Report(DiagnosticCode code, string message, SourceLocation location, VerificationContext ctx) =>
        ctx.Diagnostics.Report(code, DiagnosticSeverity.Error, message, location);
}
