using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Declarations;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Verification.Checks;

public sealed class SealedInheritanceCheck : ISemanticCheck
{
    public bool Matches(IAstNode node) => node is ClassDeclarationNode;

    public void Check(IAstNode node, VerificationContext ctx)
    {
        var @class = (ClassDeclarationNode)node;
        if (ctx.CurrentType?.Base?.IsSealed == true)
            ctx.Diagnostics.Report(
                DiagnosticCode.SealedClassInheritance,
                DiagnosticSeverity.Error,
                $"Class '{@class.Name}' cannot inherit from sealed type '{ctx.CurrentType.Base.QualifiedName}'.",
                @class.Location);
    }
}
