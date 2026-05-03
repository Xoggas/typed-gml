using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Statements;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Verification.Checks;

public sealed class DuplicateCaseCheck : ISemanticCheck
{
    public bool Matches(IAstNode node) => node is SwitchStatementNode;

    public void Check(IAstNode node, VerificationContext ctx)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var section in ((SwitchStatementNode)node).Sections.Where(section => section.Label is not null))
            if (ConstantExpressionHelper.TryValue(section.Label, ctx, new HashSet<string>(StringComparer.Ordinal), out var value) && !seen.Add(value?.ToString() ?? "null"))
                ctx.Diagnostics.Report(DiagnosticCode.DuplicateSwitchCaseLabel, DiagnosticSeverity.Error, "Duplicate switch case label.", section.Location);
    }
}
