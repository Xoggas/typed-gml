using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Declarations;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Verification.Checks;

public sealed class DuplicateParameterCheck : ISemanticCheck
{
    public bool Matches(IAstNode node) => node is MethodDeclarationNode or ConstructorDeclarationNode or DelegateDeclarationNode or LambdaExpressionNode;

    public void Check(IAstNode node, VerificationContext ctx)
    {
        var parameters = node switch
        {
            MethodDeclarationNode n => n.Parameters,
            ConstructorDeclarationNode n => n.Parameters,
            DelegateDeclarationNode n => n.Parameters,
            LambdaExpressionNode n => n.Parameters,
            _ => []
        };

        foreach (var dup in parameters.GroupBy(parameter => parameter.Name, StringComparer.Ordinal).Where(group => group.Count() > 1))
            ctx.Diagnostics.Report(DiagnosticCode.TypeMismatch, DiagnosticSeverity.Error, $"Duplicate parameter '{dup.Key}'.", dup.First().Location);
    }
}
