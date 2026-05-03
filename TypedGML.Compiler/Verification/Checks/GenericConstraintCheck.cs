using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Diagnostics;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Verification.Checks;

public sealed class GenericConstraintCheck : ISemanticCheck
{
    public bool Matches(IAstNode node) => node is ObjectCreationExpressionNode or InvocationExpressionNode or MemberAccessExpressionNode;

    public void Check(IAstNode node, VerificationContext ctx)
    {
        if (node is not ObjectCreationExpressionNode creation || !SymbolResolver.TryResolveType(creation.TypeRef, creation.TypeArgs.Count, ctx, out var type))
            return;

        if (creation.TypeArgs.Count != type.GenericParameters.Count)
        {
            Report("Wrong number of type arguments supplied.", creation.Location, ctx);
            return;
        }

        for (var i = 0; i < creation.TypeArgs.Count; i++)
            CheckConstraint(type.GenericParameters[i].Constraint, creation.TypeArgs[i], creation.Location, ctx);
    }

    private static void CheckConstraint(string? constraint, string suppliedType, SourceLocation location, VerificationContext ctx)
    {
        if (string.IsNullOrWhiteSpace(constraint) ||
            !SymbolResolver.TryResolveType(constraint, ctx, out var target) ||
            !SymbolResolver.TryResolveType(suppliedType, ctx, out var supplied))
            return;

        var ok = target.Kind switch
        {
            TypeKind.Interface => supplied.Interfaces.Contains(target),
            TypeKind.Class => TypeReferenceHelper.IsAssignable(target.QualifiedName, supplied.QualifiedName, ctx),
            TypeKind.Struct => supplied == target,
            _ => true
        };

        if (!ok)
            Report($"Type argument '{suppliedType}' does not satisfy constraint '{constraint}'.", location, ctx);
    }

    private static void Report(string message, SourceLocation location, VerificationContext ctx) =>
        ctx.Diagnostics.Report(DiagnosticCode.GenericConstraintNotSatisfied, DiagnosticSeverity.Error, message, location);
}
