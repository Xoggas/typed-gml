using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Declarations;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Verification.Checks;

public sealed class ObjectDecoratorCheck : ISemanticCheck
{
    public bool Matches(IAstNode node) => node is ClassDeclarationNode;

    public void Check(IAstNode node, VerificationContext ctx)
    {
        var @class = (ClassDeclarationNode)node;
        var decorators = @class.Decorators.Where(decorator => decorator.Name == "Object").ToList();
        if (decorators.Count > 1)
            ctx.Diagnostics.Report(DiagnosticCode.MultipleObjectDecorators, DiagnosticSeverity.Error, $"Class '{@class.Name}' has multiple @Object decorators.", @class.Location);

        if (decorators.Count == 1 &&
            (decorators[0].Args.Count != 1 || decorators[0].Args[0] is not LiteralExpressionNode { Kind: LiteralKind.String, Value: string value } || string.IsNullOrWhiteSpace(value)))
            ctx.Diagnostics.Report(DiagnosticCode.MissingObjectDecoratorName, DiagnosticSeverity.Error, $"Class '{@class.Name}' must declare a non-empty string argument for @Object.", decorators[0].Location);
    }
}
