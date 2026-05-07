using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Declarations;
using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Diagnostics;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Verification.Checks;

public sealed class MemberShadowCheck : ISemanticCheck
{
    public bool Matches(IAstNode node) => node is ClassDeclarationNode;

    public void Check(IAstNode node, VerificationContext ctx)
    {
        var @class = (ClassDeclarationNode)node;
        if (DecoratorHelper.IsBcl(@class.Location) || ctx.CurrentType?.Base is null)
            return;

        foreach (var declared in DeclaredMembers(@class.Members))
            CheckMember(declared, ctx);
    }

    private static void CheckMember(DeclaredMember declared, VerificationContext ctx)
    {
        foreach (var (ancestor, owner) in AncestorMembers(ctx.CurrentType?.Base))
        {
            if (!Conflicts(declared, ancestor))
                continue;

            if (ancestor.NativePropertyName is not null || !IsOverridable(ancestor))
            {
                ReportShadow(declared, ancestor, owner, ctx);
                return;
            }

            if (!declared.HasOverride)
                ReportMissingOverride(declared, ancestor, owner, ctx);

            return;
        }
    }

    private static IEnumerable<DeclaredMember> DeclaredMembers(IEnumerable<IAstNode> members)
    {
        foreach (var member in members)
        {
            if (member is FieldDeclarationNode field)
                yield return new(field.Name, MemberKind.Field, [], field.Modifiers, field.Location);
            else if (member is PropertyDeclarationNode property)
                yield return new(property.Name, MemberKind.Property, [], property.Modifiers, property.Location);
            else if (member is MethodDeclarationNode method)
                yield return new(method.Name, MemberKind.Method, method.Parameters.Select(p => p.TypeRef).ToList(), method.Modifiers, method.Location);
        }
    }

    private static IEnumerable<(MemberSymbol Member, TypeSymbol Owner)> AncestorMembers(TypeSymbol? type)
    {
        for (var current = type; current is not null; current = current.Base)
            foreach (var member in current.Members)
                yield return (member, current);
    }

    private static bool Conflicts(DeclaredMember declared, MemberSymbol ancestor)
    {
        if (NamesMatch(declared.Name, ancestor.NativePropertyName))
            return true;

        if (!NamesMatch(declared.Name, ancestor.Name))
            return false;

        return declared.Kind != MemberKind.Method ||
               ancestor.Kind != MemberKind.Method ||
               ParametersMatch(declared.ParameterTypes, ancestor.Parameters);
    }

    private static bool ParametersMatch(IReadOnlyList<string> declared, IReadOnlyList<ParameterSymbol> ancestor) =>
        declared.Count == ancestor.Count &&
        declared.Zip(ancestor).All(pair => string.Equals(pair.First, pair.Second.TypeRef, StringComparison.Ordinal));

    private static bool NamesMatch(string declared, string? ancestor) =>
        string.Equals(declared, ancestor, StringComparison.OrdinalIgnoreCase);

    private static bool IsOverridable(MemberSymbol member) =>
        member.Modifiers.Contains("virtual", StringComparer.Ordinal) ||
        member.Modifiers.Contains("abstract", StringComparer.Ordinal) ||
        member.Modifiers.Contains("override", StringComparer.Ordinal);

    private static void ReportMissingOverride(
        DeclaredMember declared,
        MemberSymbol ancestor,
        TypeSymbol owner,
        VerificationContext ctx) =>
        ctx.Diagnostics.Report(
            DiagnosticCode.MissingOverrideTarget,
            DiagnosticSeverity.Error,
            $"Member '{declared.Name}' hides inherited virtual or abstract member '{ancestor.Name}' from '{TypeName(owner)}'. Use override.",
            declared.Location);

    private static void ReportShadow(
        DeclaredMember declared,
        MemberSymbol ancestor,
        TypeSymbol owner,
        VerificationContext ctx) =>
        ctx.Diagnostics.Report(
            DiagnosticCode.MemberHidesInheritedMember,
            DiagnosticSeverity.Error,
            $"Member '{declared.Name}' hides inherited member '{ancestor.Name}' from '{TypeName(owner)}'. Use a different name. To intentionally hide, a language keyword would be required (not supported in TypedGML).",
            declared.Location);

    private static string TypeName(TypeSymbol type)
    {
        var dot = type.QualifiedName.LastIndexOf('.');
        return dot < 0 ? type.QualifiedName : type.QualifiedName[(dot + 1)..];
    }

    private sealed record DeclaredMember(
        string Name,
        MemberKind Kind,
        IReadOnlyList<string> ParameterTypes,
        IReadOnlyList<string> Modifiers,
        SourceLocation Location)
    {
        public bool HasOverride => Modifiers.Contains("override", StringComparer.Ordinal);
    }
}
