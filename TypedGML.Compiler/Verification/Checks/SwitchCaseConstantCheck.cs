using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Statements;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Verification.Checks;

public sealed class SwitchCaseConstantCheck : ISemanticCheck
{
    public bool Matches(IAstNode node) => node is SwitchStatementNode;

    public void Check(IAstNode node, VerificationContext ctx)
    {
        var @switch = (SwitchStatementNode)node;
        foreach (var section in @switch.Sections.Where(section => section.Label is not null))
            if (!ConstantExpressionHelper.IsConstant(section.Label, ctx))
                ctx.Diagnostics.Report(DiagnosticCode.NonConstantSwitchCaseLabel, DiagnosticSeverity.Error, "Switch case label must be a compile-time constant.", section.Location);
    }
}
