using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Ast.Statements;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Verification.Checks;

public sealed class ControlFlowCheck : ISemanticCheck
{
    public bool Matches(IAstNode node) =>
        node is BreakStatementNode or ContinueStatementNode or ReturnStatementNode or WhileStatementNode or
            ForStatementNode or RepeatStatementNode or SwitchStatementNode or BlockStatementNode or
            IfStatementNode or TernaryExpressionNode;

    public void Check(IAstNode node, VerificationContext ctx)
    {
        switch (node)
        {
            case BreakStatementNode when !ctx.IsInLoop && !ctx.IsInSwitch:
                Report(DiagnosticCode.BreakOrContinueOutsideLoopOrSwitch, "break is only valid inside a loop or switch.", node.Location, ctx);
                break;
            case ContinueStatementNode when !ctx.IsInLoop:
                Report(DiagnosticCode.BreakOrContinueOutsideLoopOrSwitch, "continue is only valid inside a loop.", node.Location, ctx);
                break;
            case WhileStatementNode loop:
                CheckBool(loop.Condition, loop.Location, ctx);
                break;
            case ForStatementNode loop when loop.Condition is not null:
                CheckBool(loop.Condition, loop.Location, ctx);
                break;
            case RepeatStatementNode loop:
                CheckNumber(loop.Count, loop.Location, ctx);
                break;
            case IfStatementNode @if:
                CheckBool(@if.Condition, @if.Location, ctx);
                break;
            case TernaryExpressionNode ternary:
                CheckBool(ternary.Condition, ternary.Location, ctx);
                break;
            case SwitchStatementNode @switch:
                CheckSwitch(@switch, ctx);
                break;
            case BlockStatementNode block:
                CheckUnreachable(block, ctx);
                break;
        }
    }

    private static void CheckSwitch(SwitchStatementNode @switch, VerificationContext ctx)
    {
        if (ctx.CurrentMember?.ReturnType != "void" && @switch.Sections.All(section => section.Label is not null))
            ctx.Diagnostics.Report(DiagnosticCode.MissingReturnInNonVoidMethod, DiagnosticSeverity.Warning, "Non-void switch should include a default section or be exhaustive.", @switch.Location);

        foreach (var section in @switch.Sections.Where(section => section.Statements.Count > 0))
            if (!Terminates(section.Statements[^1]))
                Report(DiagnosticCode.TypeMismatch, "Switch section falls through without termination.", section.Location, ctx);
    }

    private static void CheckUnreachable(BlockStatementNode block, VerificationContext ctx)
    {
        var terminated = false;
        foreach (var statement in block.Statements)
        {
            if (terminated)
                ctx.Diagnostics.Report(DiagnosticCode.TypeMismatch, DiagnosticSeverity.Warning, "Statement is unreachable.", statement.Location);

            terminated |= Terminates(statement);
        }
    }

    private static bool Terminates(IAstNode node) =>
        node is ReturnStatementNode or BreakStatementNode or ContinueStatementNode or ThrowStatementNode;

    private static void CheckBool(IAstNode condition, SourceLocation location, VerificationContext ctx)
    {
        var type = ExpressionTypeResolver.Resolve(condition, ctx);
        if (type == "bool")
            return;

        Report(type == "number" ? DiagnosticCode.ImplicitNumberBoolConversion : DiagnosticCode.TypeMismatch, "Condition must be of type bool.", location, ctx);
    }

    private static void CheckNumber(IAstNode expression, SourceLocation location, VerificationContext ctx)
    {
        if (ExpressionTypeResolver.Resolve(expression, ctx) != "number")
            Report(DiagnosticCode.TypeMismatch, "repeat count must be of type number.", location, ctx);
    }

    private static void Report(DiagnosticCode code, string message, SourceLocation location, VerificationContext ctx) =>
        ctx.Diagnostics.Report(code, DiagnosticSeverity.Error, message, location);
}
