using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Ast.Statements;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Verification.Checks;

public sealed class DictionaryLiteralCheck : ISemanticCheck
{
    public bool Matches(IAstNode node) => node is DictionaryLiteralExpressionNode or AssignmentExpressionNode or VarDeclarationStatementNode;

    public void Check(IAstNode node, VerificationContext ctx)
    {
        if (node is DictionaryLiteralExpressionNode literal)
            CheckEntries(literal, ctx);
        else if (node is AssignmentExpressionNode assignment && assignment.Value is DictionaryLiteralExpressionNode value)
            CheckTarget(ExpressionTypeResolver.Resolve(assignment.Target, ctx), value.Location, ctx);
        else if (node is VarDeclarationStatementNode declaration && declaration.Initializer is DictionaryLiteralExpressionNode init)
            CheckTarget(declaration.TypeRef, init.Location, ctx);
    }

    private static void CheckEntries(DictionaryLiteralExpressionNode literal, VerificationContext ctx)
    {
        foreach (var entry in literal.Entries)
            if (!ConstantExpressionHelper.IsConstant(entry.Key, ctx))
                Report("Dictionary literal keys must be compile-time constants.", entry.Location, ctx);
    }

    private static void CheckTarget(string? targetType, SourceLocation location, VerificationContext ctx)
    {
        if (TypeReferenceHelper.RootName(targetType) != "Dictionary")
            Report("Dictionary literals are only valid when the target type is Dictionary<K,V>.", location, ctx);
    }

    private static void Report(string message, SourceLocation location, VerificationContext ctx) =>
        ctx.Diagnostics.Report(DiagnosticCode.InvalidDictionaryLiteralTarget, DiagnosticSeverity.Error, message, location);
}
