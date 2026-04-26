using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Bcl;
using TypedGML.Compiler.Diagnostics;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Verification.Checks;

public sealed class OperatorCheck(
    Func<string, string, string, string?> resolveBinary,
    Func<string, string, string?> resolveUnary) : ISemanticCheck
{
    public OperatorCheck() : this(PrimitiveOperationRegistry.ResolveResultType, PrimitiveOperationRegistry.ResolveUnaryResultType) { }

    public bool Matches(IAstNode node) => node is BinaryExpressionNode or UnaryExpressionNode or CastExpressionNode;

    public void Check(IAstNode node, VerificationContext ctx)
    {
        if (node is BinaryExpressionNode binary)
            CheckBinary(binary, ctx);
        else if (node is UnaryExpressionNode unary)
            CheckUnary(unary, ctx);
        else
            CheckCast((CastExpressionNode)node, ctx);
    }

    private void CheckBinary(BinaryExpressionNode binary, VerificationContext ctx)
    {
        var leftType = ExpressionTypeResolver.Resolve(binary.Left, ctx) ?? string.Empty;
        var rightType = ExpressionTypeResolver.Resolve(binary.Right, ctx) ?? string.Empty;
        if (resolveBinary(binary.Op, leftType, rightType) is not null || HasBinaryOverload(binary.Op, leftType, rightType, ctx))
            return;

        Report(DiagnosticCode.TypeMismatch, $"Operator '{binary.Op}' cannot be applied to '{leftType}' and '{rightType}'.", binary.Location, ctx);
    }

    private void CheckUnary(UnaryExpressionNode unary, VerificationContext ctx)
    {
        var operandType = ExpressionTypeResolver.Resolve(unary.Operand, ctx) ?? string.Empty;
        if (resolveUnary(unary.Op, operandType) is not null || HasUnaryOverload(unary.Op, operandType, ctx))
            return;

        Report(DiagnosticCode.TypeMismatch, $"Operator '{unary.Op}' cannot be applied to '{operandType}'.", unary.Location, ctx);
    }

    private static void CheckCast(CastExpressionNode cast, VerificationContext ctx)
    {
        var sourceType = ExpressionTypeResolver.Resolve(cast.Expression, ctx) ?? string.Empty;
        if (TypeReferenceHelper.IsAssignable(cast.TargetType, sourceType, ctx))
            return;

        if (HasConversion(cast.TargetType, sourceType, ctx, implicitOnly: false))
            ctx.Diagnostics.Report(DiagnosticCode.TypeMismatch, DiagnosticSeverity.Warning, $"Cast from '{sourceType}' to '{cast.TargetType}' may be lossy.", cast.Location);
        else
            Report(DiagnosticCode.TypeMismatch, $"Cannot cast '{sourceType}' to '{cast.TargetType}'.", cast.Location, ctx);
    }

    private static bool HasBinaryOverload(string op, string leftType, string rightType, VerificationContext ctx) =>
        HasOperator(leftType, op, rightType, ctx) || HasOperator(rightType, op, leftType, ctx);

    private static bool HasUnaryOverload(string op, string operandType, VerificationContext ctx) =>
        HasOperator(operandType, op, operandType, ctx, unary: true);

    private static bool HasOperator(string ownerType, string op, string argType, VerificationContext ctx, bool unary = false)
    {
        if (!SymbolResolver.TryResolveType(ownerType, ctx, out var symbol))
            return false;

        return symbol.Members.Any(member =>
            member.Kind == MemberKind.Operator &&
            member.Name == op &&
            member.Parameters.Count == (unary ? 1 : 2) &&
            member.Parameters.Last().TypeRef == argType);
    }

    private static bool HasConversion(string targetType, string sourceType, VerificationContext ctx, bool implicitOnly)
    {
        if (!SymbolResolver.TryResolveType(sourceType, ctx, out var source) || !SymbolResolver.TryResolveType(targetType, ctx, out var target))
            return false;

        return TypeReferenceHelper.HasConversion(source, sourceType, targetType, implicitOnly) ||
               TypeReferenceHelper.HasConversion(target, sourceType, targetType, implicitOnly);
    }

    private static void Report(DiagnosticCode code, string message, SourceLocation location, VerificationContext ctx) =>
        ctx.Diagnostics.Report(code, DiagnosticSeverity.Error, message, location);
}
