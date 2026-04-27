using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Diagnostics;
using TypedGML.Compiler.Emission;

namespace TypedGML.Compiler.Verification.Checks;

public sealed class NativeEventNameCheck : ISemanticCheck
{
    public bool Matches(IAstNode node) => node is MethodDeclarationNode;

    public void Check(IAstNode node, VerificationContext ctx)
    {
        var method = (MethodDeclarationNode)node;
        var decorator = method.Decorators.FirstOrDefault(d => d.Name == "NativeEvent");
        if (decorator?.Args.FirstOrDefault() is not LiteralExpressionNode { Kind: LiteralKind.String, Value: string eventName })
            return;

        try
        {
            GmlEventMap.Resolve(eventName);
        }
        catch (InvalidOperationException ex) when (ex.Message.StartsWith("TGML0035:", StringComparison.Ordinal))
        {
            ctx.Diagnostics.Report(
                DiagnosticCode.UnknownNativeEventName,
                DiagnosticSeverity.Error,
                ex.Message["TGML0035: ".Length..],
                decorator.Location);
        }
    }
}
