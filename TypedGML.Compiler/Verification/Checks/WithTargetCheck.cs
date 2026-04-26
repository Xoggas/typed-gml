using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Statements;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Verification.Checks;

public sealed class WithTargetCheck : ISemanticCheck
{
    public bool Matches(IAstNode node) => node is WithStatementNode;

    public void Check(IAstNode node, VerificationContext ctx)
    {
        var with = (WithStatementNode)node;
        var typeRef = ExpressionTypeResolver.Resolve(with.Target, ctx);
        if (!SymbolResolver.TryResolveType(typeRef, ctx, out var type) || string.IsNullOrWhiteSpace(type.ObjectAssetName))
            ctx.Diagnostics.Report(DiagnosticCode.WithTargetNotObjectType, DiagnosticSeverity.Error, "with target must be an @Object type.", with.Location);
    }
}
