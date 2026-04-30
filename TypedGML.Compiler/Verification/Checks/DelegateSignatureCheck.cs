using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Diagnostics;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Verification.Checks;

public sealed class DelegateSignatureCheck : ISemanticCheck
{
    public bool Matches(IAstNode node) => node is AssignmentExpressionNode or InvocationExpressionNode;

    public void Check(IAstNode node, VerificationContext ctx)
    {
        if (node is AssignmentExpressionNode assignment)
            CheckSubscription(assignment, ctx);
        else
            CheckInvocation((InvocationExpressionNode)node, ctx);
    }

    private static void CheckSubscription(AssignmentExpressionNode assignment, VerificationContext ctx)
    {
        if (assignment.Op is not "+=" and not "-=")
            return;

        var targetType = ExpressionTypeResolver.Resolve(assignment.Target, ctx);
        if (IsNonDelegateType(targetType, ctx))
            return;

        if (!DelegateTypeHelper.TrySignature(targetType, ctx, out var returnType, out var parameterTypes) &&
            !IsEvent(assignment.Target, ctx))
        {
            Report("+= and -= require a delegate or event target.", assignment.Location, ctx);
            return;
        }

        if (assignment.Value is LambdaExpressionNode lambda && !MatchesLambda(lambda, returnType, parameterTypes))
            Report("Lambda does not match the target delegate signature.", assignment.Location, ctx);

        if (assignment.Value is IdentifierExpressionNode identifier)
        {
            var method = MemberSignatureHelper.Members(ctx.CurrentType, identifier.Name, MemberKind.Method).FirstOrDefault();
            if (method is null || method.ReturnType != returnType || !method.Parameters.Select(p => p.TypeRef).SequenceEqual(parameterTypes))
                Report("Method does not match the target delegate signature.", assignment.Location, ctx);
        }
    }

    private static void CheckInvocation(InvocationExpressionNode invocation, VerificationContext ctx)
    {
        var targetType = ExpressionTypeResolver.Resolve(invocation.Target, ctx);
        if (!DelegateTypeHelper.TrySignature(targetType, ctx, out _, out var parameterTypes))
            return;

        if (invocation.PositionalArgs.Count != parameterTypes.Count)
            Report("Delegate invocation has the wrong argument count.", invocation.Location, ctx);

        for (var i = 0; i < Math.Min(invocation.PositionalArgs.Count, parameterTypes.Count); i++)
            if (!TypeReferenceHelper.IsAssignable(parameterTypes[i], ExpressionTypeResolver.Resolve(invocation.PositionalArgs[i], ctx), ctx))
                Report("Delegate invocation argument type mismatch.", invocation.Location, ctx);
    }

    private static bool IsNonDelegateType(string? targetType, VerificationContext ctx)
    {
        if (string.IsNullOrEmpty(targetType))
            return true;
        if (targetType is "number" or "string" or "bool" or "object" or "void")
            return true;
        return SymbolResolver.TryResolveType(targetType, ctx, out var symbol) && symbol.Kind != TypeKind.Delegate;
    }

    private static bool IsEvent(IAstNode target, VerificationContext ctx)
    {
        var name = target switch
        {
            IdentifierExpressionNode id => id.Name,
            MemberAccessExpressionNode access => access.MemberName,
            _ => string.Empty
        };
        return MemberSignatureHelper.Members(ctx.CurrentType, name, MemberKind.Event).Any();
    }

    private static bool MatchesLambda(LambdaExpressionNode lambda, string returnType, IReadOnlyList<string> parameterTypes) =>
        lambda.Parameters.Count == parameterTypes.Count &&
        lambda.Parameters.Zip(parameterTypes).All(pair => string.IsNullOrEmpty(pair.First.TypeRef) || pair.First.TypeRef == pair.Second);

    private static void Report(string message, SourceLocation location, VerificationContext ctx) =>
        ctx.Diagnostics.Report(DiagnosticCode.DelegateSignatureMismatch, DiagnosticSeverity.Error, message, location);
}
