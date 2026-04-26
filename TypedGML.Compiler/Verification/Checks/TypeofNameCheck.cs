using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Verification.Checks;

public sealed class TypeofNameCheck : ISemanticCheck
{
    public bool Matches(IAstNode node) => node is TypeofExpressionNode or NameofExpressionNode;

    public void Check(IAstNode node, VerificationContext ctx)
    {
        if (node is TypeofExpressionNode typeOf)
        {
            if (!SymbolResolver.TryResolveType(typeOf.TypeName, ctx, out _))
                Report($"Unknown type '{typeOf.TypeName}'.", typeOf.Location, ctx);
            return;
        }

        ResolveChain((NameofExpressionNode)node, ctx);
    }

    private static void ResolveChain(NameofExpressionNode nameofExpr, VerificationContext ctx)
    {
        if (nameofExpr.Chain.Count == 0)
            return;

        string? currentType = null;
        foreach (var segment in nameofExpr.Chain)
        {
            if (currentType is null)
            {
                if (ctx.Scope.TryResolve(segment, out currentType))
                    continue;
                if (SymbolResolver.TryResolveType(segment, ctx, out var type))
                {
                    currentType = type.QualifiedName;
                    continue;
                }

                var member = SymbolResolver.FindMember(ctx.CurrentType, segment, out _);
                currentType = member?.ReturnType;
                if (currentType is not null)
                    continue;
            }
            else if (SymbolResolver.TryResolveType(currentType, ctx, out var target) &&
                     SymbolResolver.FindMember(target, segment, out _) is { } member)
            {
                currentType = member.ReturnType;
                continue;
            }

            Report($"nameof segment '{segment}' could not be resolved.", nameofExpr.Location, ctx);
            return;
        }
    }

    private static void Report(string message, SourceLocation location, VerificationContext ctx) =>
        ctx.Diagnostics.Report(DiagnosticCode.TypeMismatch, DiagnosticSeverity.Error, message, location);
}
