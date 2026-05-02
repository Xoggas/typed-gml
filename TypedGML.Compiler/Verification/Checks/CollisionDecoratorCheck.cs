using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Diagnostics;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Verification.Checks;

public sealed class CollisionDecoratorCheck : ISemanticCheck
{
    public bool Matches(IAstNode node) =>
        node is MethodDeclarationNode method && method.Decorators.Any(d => d.Name == "Collision");

    public void Check(IAstNode node, VerificationContext ctx)
    {
        var method = (MethodDeclarationNode)node;
        foreach (var decorator in method.Decorators.Where(d => d.Name == "Collision"))
            CheckDecorator(decorator, ctx);
    }

    private static void CheckDecorator(DecoratorNode decorator, VerificationContext ctx)
    {
        if (decorator.Args.Count != 1 || decorator.Args[0] is not TypeofExpressionNode typeOf)
        {
            ctx.Diagnostics.Report(
                DiagnosticCode.CollisionArgumentMustBeTypeof,
                DiagnosticSeverity.Error,
                "@Collision argument must be typeof(TypeName).",
                decorator.Location);
            return;
        }

        if (!SymbolResolver.TryResolveType(typeOf.TypeName, ctx, out var targetType))
        {
            ctx.Diagnostics.Report(DiagnosticCode.TypeMismatch, DiagnosticSeverity.Error, $"Unknown type '{typeOf.TypeName}'.", typeOf.Location);
            return;
        }

        if (targetType.Kind != TypeKind.Class || string.IsNullOrEmpty(targetType.ObjectAssetName))
            ctx.Diagnostics.Report(
                DiagnosticCode.CollisionTargetMissingObjectDecorator,
                DiagnosticSeverity.Error,
                $"@Collision target type '{targetType.QualifiedName}' is not decorated with @Object.",
                typeOf.Location);
    }
}
