using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;

namespace TypedGML.Compiler.Verification;

internal static class AssignmentNarrowingHelper
{
    public static AssignmentNarrowing Capture(IAstNode node, VerificationContext ctx)
    {
        if (node is not AssignmentExpressionNode { Op: "=", Target: IdentifierExpressionNode target } assignment)
            return AssignmentNarrowing.None;

        var targetType = ResolveDeclaredIdentifierType(target.Name, ctx);
        if (!TypeReferenceHelper.IsNullable(targetType))
            return AssignmentNarrowing.None;

        var sourceType = ExpressionTypeResolver.Resolve(assignment.Value, ctx);
        var narrowedType = TypeReferenceHelper.UnwrapNullable(targetType);
        return TypeReferenceHelper.IsAssignable(narrowedType, sourceType, ctx)
            ? new AssignmentNarrowing(target.Name, narrowedType)
            : AssignmentNarrowing.None;
    }

    public static void Apply(AssignmentNarrowing narrowing, VerificationContext ctx)
    {
        if (narrowing.HasValue)
            ctx.NarrowVariable(narrowing.Name, narrowing.TypeRef);
    }

    private static string? ResolveDeclaredIdentifierType(string name, VerificationContext ctx)
    {
        if (ctx.Scope.TryResolve(name, out var scopedType))
            return scopedType;

        return SymbolResolver.FindMember(ctx.CurrentType, name, out _)?.ReturnType;
    }
}
