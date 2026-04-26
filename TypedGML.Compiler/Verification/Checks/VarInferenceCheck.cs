using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Ast.Statements;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Verification.Checks;

public sealed class VarInferenceCheck : ISemanticCheck
{
    public bool Matches(IAstNode node) => node is VarDeclarationStatementNode;

    public void Check(IAstNode node, VerificationContext ctx)
    {
        var declaration = (VarDeclarationStatementNode)node;
        if (!declaration.IsVar)
            return;

        if (declaration.Initializer is LiteralExpressionNode { Kind: LiteralKind.Null })
            Report("Cannot infer a var type from null.", declaration.Location, ctx);
        else if (string.IsNullOrWhiteSpace(ExpressionTypeResolver.Resolve(declaration.Initializer, ctx)))
            Report("Cannot infer a var type from the initializer.", declaration.Location, ctx);
    }

    private static void Report(string message, SourceLocation location, VerificationContext ctx) =>
        ctx.Diagnostics.Report(DiagnosticCode.AmbiguousVarInference, DiagnosticSeverity.Error, message, location);
}
