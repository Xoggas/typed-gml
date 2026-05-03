using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Declarations;
using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Verification.Checks;

public sealed class DecoratorPlacementCheck : ISemanticCheck
{
    public bool Matches(IAstNode node) =>
        node is ClassDeclarationNode or StructDeclarationNode or InterfaceDeclarationNode or EnumDeclarationNode or
            DelegateDeclarationNode or FunctionDeclarationNode or FieldDeclarationNode or PropertyDeclarationNode or
            IndexerDeclarationNode or MethodDeclarationNode or ConstructorDeclarationNode or OperatorDeclarationNode or
            ConversionOperatorNode or EventDeclarationNode;

    public void Check(IAstNode node, VerificationContext ctx)
    {
        foreach (var decorator in DecoratorHelper.Decorators(node))
        {
            if (decorator.Name == "Collision")
            {
                if (node is not MethodDeclarationNode || string.IsNullOrEmpty(ctx.CurrentType?.ObjectAssetName))
                    ctx.Diagnostics.Report(
                        DiagnosticCode.CollisionDecoratorInvalidTarget,
                        DiagnosticSeverity.Error,
                        "@Collision can only be applied to methods in @Object classes.",
                        decorator.Location);
                continue;
            }

            if (!Valid(decorator.Name, node))
                ctx.Diagnostics.Report(DiagnosticCode.TypeMismatch, DiagnosticSeverity.Error, $"Decorator '@{decorator.Name}' is not valid on {node.GetType().Name}.", decorator.Location);
        }
    }

    private static bool Valid(string name, IAstNode node) => name switch
    {
        "Object" => node is ClassDeclarationNode,
        "NativeEvent" => node is MethodDeclarationNode,
        "Collision" => node is MethodDeclarationNode,
        "NativeProperty" => node is PropertyDeclarationNode,
        "NativeCall" => node is MethodDeclarationNode or OperatorDeclarationNode or ConversionOperatorNode,
        "Asset" => node is PropertyDeclarationNode,
        _ => true
    };
}
