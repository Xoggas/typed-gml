using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Declarations;
using TypedGML.Compiler.Diagnostics;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Verification.Checks;

public sealed class InterfaceImplementationCheck : ISemanticCheck
{
    public bool Matches(IAstNode node) => node is ClassDeclarationNode or StructDeclarationNode;

    public void Check(IAstNode node, VerificationContext ctx)
    {
        foreach (var @interface in Interfaces(ctx.CurrentType))
            foreach (var member in @interface.Members)
                if (!MemberSignatureHelper.Members(ctx.CurrentType, member.Name, member.Kind).Any(candidate => MemberSignatureHelper.SignatureExact(candidate, member)))
                    Report(DiagnosticCode.InterfaceMemberNotImplemented, $"Type '{ctx.CurrentType?.QualifiedName}' must implement '{@interface.QualifiedName}.{member.Name}'.", node.Location, ctx);
    }

    private static IEnumerable<TypeSymbol> Interfaces(TypeSymbol? type)
    {
        if (type is null)
            yield break;

        foreach (var @interface in type.Interfaces)
        {
            yield return @interface;
            foreach (var inherited in Interfaces(@interface))
                yield return inherited;
        }
    }

    private static void Report(DiagnosticCode code, string message, SourceLocation location, VerificationContext ctx) =>
        ctx.Diagnostics.Report(code, DiagnosticSeverity.Error, message, location);
}
