using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Declarations;
using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Verification.Checks;

public sealed class StaticModifierCheck : ISemanticCheck
{
    public bool Matches(IAstNode node) =>
        node is ConstructorDeclarationNode or IndexerDeclarationNode or InterfaceDeclarationNode;

    public void Check(IAstNode node, VerificationContext ctx)
    {
        switch (node)
        {
            case ConstructorDeclarationNode ctor when ctor.Modifiers.Contains("static", StringComparer.Ordinal):
                Report("constructors cannot declare the static modifier.", ctor.Location, ctx, DiagnosticCode.InvalidStaticModifierTarget);
                break;
            case IndexerDeclarationNode indexer when indexer.Modifiers.Contains("static", StringComparer.Ordinal):
                Report("indexers cannot declare the static modifier.", indexer.Location, ctx, DiagnosticCode.InvalidStaticModifierTarget);
                break;
            case InterfaceDeclarationNode iface:
                foreach (var member in iface.Members.Where(HasStaticModifier))
                    Report("interface members cannot declare the static modifier.", member.Location, ctx, DiagnosticCode.StaticMemberInInterface);
                break;
        }
    }

    private static bool HasStaticModifier(IAstNode node) => Modifiers(node).Contains("static", StringComparer.Ordinal);

    private static IReadOnlyList<string> Modifiers(IAstNode node) => node switch
    {
        PropertyDeclarationNode n => n.Modifiers,
        IndexerDeclarationNode n => n.Modifiers,
        MethodDeclarationNode n => n.Modifiers,
        ConstructorDeclarationNode n => n.Modifiers,
        EventDeclarationNode n => n.Modifiers,
        _ => []
    };

    private static void Report(string message, SourceLocation location, VerificationContext ctx, DiagnosticCode code) =>
        ctx.Diagnostics.Report(code, DiagnosticSeverity.Error, message, location);
}
