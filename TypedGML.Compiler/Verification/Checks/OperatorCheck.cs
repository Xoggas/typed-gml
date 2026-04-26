using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Verification.Checks;

public sealed class OperatorCheck : ISemanticCheck
{
    public bool Matches(IAstNode node) => node is BinaryExpressionNode or UnaryExpressionNode;

    public void Check(IAstNode node, VerificationContext ctx)
    {
        switch (node)
        {
            case BinaryExpressionNode binary:
                CheckBinary(binary, ctx);
                break;
            case UnaryExpressionNode unary:
                CheckUnary(unary, ctx);
                break;
        }
    }

    private static void CheckBinary(BinaryExpressionNode binary, VerificationContext ctx)
    {
        var leftType = ExpressionTypeResolver.Resolve(binary.Left, ctx);
        var rightType = ExpressionTypeResolver.Resolve(binary.Right, ctx);
        if (binary.Op is "and" or "or")
        {
            CheckBool(binary.Left, leftType, $"Left operand of '{binary.Op}' must be bool.", ctx);
            CheckBool(binary.Right, rightType, $"Right operand of '{binary.Op}' must be bool.", ctx);
            return;
        }

        if (binary.Op is "==" or "!=")
        {
            if (OperatorResolutionHelper.FindBinary(binary.Op, leftType, rightType, ctx) is not null)
                return;

            return;
        }

        if (OperatorResolutionHelper.FindBinary(binary.Op, leftType, rightType, ctx) is not null)
            return;

        Report($"Operator '{binary.Op}' cannot be applied to '{leftType ?? "unknown"}' and '{rightType ?? "unknown"}'.", binary.Location, ctx);
    }

    private static void CheckUnary(UnaryExpressionNode unary, VerificationContext ctx)
    {
        var operandType = ExpressionTypeResolver.Resolve(unary.Operand, ctx);
        if (unary.Op == "not")
        {
            CheckBool(unary.Operand, operandType, "Operand of 'not' must be bool.", ctx);
            return;
        }

        if (OperatorResolutionHelper.FindUnary(unary.Op, operandType, ctx) is not null)
            return;

        Report($"Operator '{unary.Op}' cannot be applied to '{operandType ?? "unknown"}'.", unary.Location, ctx);
    }

    private static void CheckBool(IAstNode operand, string? type, string message, VerificationContext ctx)
    {
        if (type == "bool")
            return;

        var code = type == "number" ? DiagnosticCode.ImplicitNumberBoolConversion : DiagnosticCode.TypeMismatch;
        ctx.Diagnostics.Report(code, DiagnosticSeverity.Error, message, operand.Location);
    }

    private static void Report(string message, SourceLocation location, VerificationContext ctx) =>
        ctx.Diagnostics.Report(DiagnosticCode.TypeMismatch, DiagnosticSeverity.Error, message, location);
}
