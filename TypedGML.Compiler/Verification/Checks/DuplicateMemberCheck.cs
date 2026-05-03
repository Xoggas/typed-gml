using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Declarations;
using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Verification.Checks;

public sealed class DuplicateMemberCheck : ISemanticCheck
{
    public bool Matches(IAstNode node) => node is ClassDeclarationNode or StructDeclarationNode or InterfaceDeclarationNode;

    public void Check(IAstNode node, VerificationContext ctx)
    {
        var members = node switch
        {
            ClassDeclarationNode n => n.Members,
            StructDeclarationNode n => n.Members,
            InterfaceDeclarationNode n => n.Members,
            _ => []
        };

        CheckDuplicates(members.OfType<FieldDeclarationNode>().Select(member => (member.Name, member.Location)));
        CheckDuplicates(members.OfType<PropertyDeclarationNode>().Select(member => (member.Name, member.Location)));
        CheckDuplicates(members.OfType<EventDeclarationNode>().Select(member => (member.Name, member.Location)));
        CheckMethods(members.OfType<MethodDeclarationNode>(), ctx);

        void CheckDuplicates(IEnumerable<(string Name, SourceLocation Location)> group)
        {
            foreach (var dup in group.GroupBy(member => member.Name, StringComparer.Ordinal).Where(g => g.Count() > 1))
                ctx.Diagnostics.Report(DiagnosticCode.TypeMismatch, DiagnosticSeverity.Error, $"Duplicate member '{dup.Key}'.", dup.First().Location);
        }
    }

    private static void CheckMethods(IEnumerable<MethodDeclarationNode> methods, VerificationContext ctx)
    {
        foreach (var dup in methods.GroupBy(method => $"{method.Name}({string.Join(",", method.Parameters.Select(p => p.TypeRef))})", StringComparer.Ordinal).Where(g => g.Count() > 1))
            ctx.Diagnostics.Report(DiagnosticCode.TypeMismatch, DiagnosticSeverity.Error, $"Duplicate method overload '{dup.First().Name}'.", dup.First().Location);
    }
}
