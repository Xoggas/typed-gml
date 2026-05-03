using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Verification.Checks;

public sealed class DefaultParameterConstCheck : ISemanticCheck
{
    public bool Matches(IAstNode node) => node is MethodDeclarationNode or ConstructorDeclarationNode;

    public void Check(IAstNode node, VerificationContext ctx)
    {
        var parameters = node is MethodDeclarationNode method ? method.Parameters : ((ConstructorDeclarationNode)node).Parameters;
        var seenDefault = false;
        foreach (var parameter in parameters)
        {
            if (parameter.DefaultValue is null)
            {
                if (seenDefault)
                    Report("Parameters without defaults cannot follow parameters with defaults.", parameter.Location, ctx);
                continue;
            }

            seenDefault = true;
            if (!ConstantExpressionHelper.IsConstant(parameter.DefaultValue, ctx))
                Report("Default parameter value must be a literal or const reference.", parameter.Location, ctx);
        }
    }

    private static void Report(string message, SourceLocation location, VerificationContext ctx) =>
        ctx.Diagnostics.Report(DiagnosticCode.NonConstantDefaultParameter, DiagnosticSeverity.Error, message, location);
}
