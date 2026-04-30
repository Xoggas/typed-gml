using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Diagnostics;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Verification.Checks;

public sealed class MethodCallCheck : ISemanticCheck
{
    public bool Matches(IAstNode node) => node is InvocationExpressionNode;

    public void Check(IAstNode node, VerificationContext ctx)
    {
        var invocation = (InvocationExpressionNode)node;
        if (HasDuplicateNamedArgs(invocation))
        {
            Report(DiagnosticCode.UnknownNamedArgument, "Named arguments must not be duplicated.", invocation.Location, ctx);
            return;
        }

        var candidates = InvocationResolver.ResolveCandidates(invocation, ctx, out var delegateInvocation);
        if (candidates.Count == 0)
        {
            if (!delegateInvocation)
                Report(DiagnosticCode.TypeMismatch, "Target expression is not callable.", invocation.Location, ctx);
            return;
        }

        if (!ValidateNames(invocation, candidates, ctx))
            return;

        var matches = candidates.Where(candidate => Matches(candidate, invocation, ctx)).ToList();
        if (matches.Count == 0)
            Report(DiagnosticCode.NoMatchingMethodOverload, "No overload matches the supplied arguments.", invocation.Location, ctx);
        else if (matches.Count > 1)
            Report(DiagnosticCode.AmbiguousMethodCall, "Call is ambiguous between multiple overloads.", invocation.Location, ctx);
    }

    private static bool ValidateNames(InvocationExpressionNode invocation, IReadOnlyList<MemberSymbol> candidates, VerificationContext ctx)
    {
        foreach (var namedArg in invocation.NamedArgs)
            if (!candidates.Any(candidate => candidate.Parameters.Any(parameter => parameter.Name == namedArg.Name)))
            {
                Report(DiagnosticCode.UnknownNamedArgument, $"Unknown named argument '{namedArg.Name}'.", namedArg.Location, ctx);
                return false;
            }

        foreach (var candidate in candidates)
            foreach (var namedArg in invocation.NamedArgs)
            {
                var index = IndexOf(candidate, namedArg.Name);
                if (index >= 0 && index < invocation.PositionalArgs.Count)
                {
                    Report(DiagnosticCode.UnknownNamedArgument, $"Named argument '{namedArg.Name}' duplicates a positional argument.", namedArg.Location, ctx);
                    return false;
                }
            }

        return true;
    }

    private static bool Matches(MemberSymbol candidate, InvocationExpressionNode invocation, VerificationContext ctx)
    {
        if (invocation.PositionalArgs.Count > candidate.Parameters.Count)
            return false;

        var suppliedCount = invocation.PositionalArgs.Count + invocation.NamedArgs.Count;
        if (suppliedCount < MemberSignatureHelper.RequiredParameters(candidate) || suppliedCount > candidate.Parameters.Count)
            return false;

        for (var i = 0; i < invocation.PositionalArgs.Count; i++)
            if (!ArgumentMatches(candidate.Parameters[i].TypeRef, invocation.PositionalArgs[i], ctx))
                return false;

        foreach (var namedArg in invocation.NamedArgs)
        {
            var index = IndexOf(candidate, namedArg.Name);
            if (index < 0)
                return false;

            if (!ArgumentMatches(candidate.Parameters[index].TypeRef, namedArg.Value, ctx))
                return false;
        }

        return true;
    }

    private static bool ArgumentMatches(string targetType, IAstNode value, VerificationContext ctx) =>
        value is LambdaExpressionNode lambda && DelegateTypeHelper.TrySignature(targetType, ctx, out _, out var parameterTypes)
            ? LambdaParametersMatch(lambda, parameterTypes)
            : TypeReferenceHelper.IsAssignable(targetType, ExpressionTypeResolver.Resolve(value, ctx), ctx);

    private static bool LambdaParametersMatch(LambdaExpressionNode lambda, IReadOnlyList<string> parameterTypes) =>
        lambda.Parameters.Count == parameterTypes.Count &&
        lambda.Parameters.Zip(parameterTypes).All(pair => string.IsNullOrEmpty(pair.First.TypeRef) || pair.First.TypeRef == pair.Second);

    private static bool HasDuplicateNamedArgs(InvocationExpressionNode invocation) =>
        invocation.NamedArgs.GroupBy(arg => arg.Name, StringComparer.Ordinal).Any(group => group.Count() > 1);

    private static int IndexOf(MemberSymbol candidate, string name)
    {
        for (var i = 0; i < candidate.Parameters.Count; i++)
            if (candidate.Parameters[i].Name == name)
                return i;

        return -1;
    }

    private static void Report(DiagnosticCode code, string message, SourceLocation location, VerificationContext ctx) =>
        ctx.Diagnostics.Report(code, DiagnosticSeverity.Error, message, location);
}
