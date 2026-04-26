using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Declarations;
using TypedGML.Compiler.Diagnostics;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Verification.Checks;

public sealed class GameObjectDecoratorCheck : ISemanticCheck
{
    private const string GameObjectQualifiedName = "TypedGML.GameObjects.GameObject";

    public bool Matches(IAstNode node) => node is ClassDeclarationNode;

    public void Check(IAstNode node, VerificationContext ctx)
    {
        var @class = (ClassDeclarationNode)node;
        if (DecoratorHelper.IsBcl(@class.Location))
            return;

        var hasObjectDecorator = ctx.CurrentType?.ObjectAssetName is not null;
        var extendsGameObject = ExtendsGameObject(ctx.CurrentType);

        if (extendsGameObject && !hasObjectDecorator)
            ctx.Diagnostics.Report(
                DiagnosticCode.GameObjectMissingObjectDecorator,
                DiagnosticSeverity.Error,
                $"Class '{@class.Name}' extends GameObject and must be decorated with @Object.",
                @class.Location);

        if (hasObjectDecorator && !extendsGameObject)
            ctx.Diagnostics.Report(
                DiagnosticCode.ObjectDecoratorWithoutGameObject,
                DiagnosticSeverity.Error,
                $"@Object class '{@class.Name}' must explicitly extend TypedGML.GameObjects.GameObject.",
                @class.Location);
    }

    private static bool ExtendsGameObject(TypeSymbol? type)
    {
        for (var current = type?.Base; current is not null; current = current.Base)
            if (string.Equals(current.QualifiedName, GameObjectQualifiedName, StringComparison.Ordinal))
                return true;
        return false;
    }
}

