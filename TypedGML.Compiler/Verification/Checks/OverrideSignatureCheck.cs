using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Diagnostics;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Verification.Checks;

public sealed class OverrideSignatureCheck : ISemanticCheck
{
    public bool Matches(IAstNode node) => node is MethodDeclarationNode or PropertyDeclarationNode;

    public void Check(IAstNode node, VerificationContext ctx)
    {
        if (node is MethodDeclarationNode method)
            CheckMethod(method, ctx);
        else
            CheckProperty((PropertyDeclarationNode)node, ctx);
    }

    private static void CheckMethod(MethodDeclarationNode method, VerificationContext ctx)
    {
        if (ctx.CurrentType?.Kind == TypeKind.Struct && HasAny(method.Modifiers, "virtual", "override"))
            Report(DiagnosticCode.TypeMismatch, $"Struct member '{method.Name}' cannot be virtual or override.", method.Location, ctx);

        if (!method.Modifiers.Contains("override", StringComparer.Ordinal))
            return;

        var ancestors = MemberSignatureHelper.Members(ctx.CurrentType?.Base, method.Name, MemberKind.Method).ToList();
        var exact = ancestors.FirstOrDefault(candidate => MemberSignatureHelper.SignatureExact(candidate, method));
        if (exact is null || !HasAny(exact.Modifiers, "virtual", "abstract"))
            Report(DiagnosticCode.MissingOverrideTarget, $"Method '{method.Name}' does not override a compatible virtual or abstract member.", method.Location, ctx);
        else if (exact.Modifiers.Contains("sealed", StringComparer.Ordinal))
            Report(DiagnosticCode.TypeMismatch, $"Method '{method.Name}' cannot override a sealed member.", method.Location, ctx);
    }

    private static void CheckProperty(PropertyDeclarationNode property, VerificationContext ctx)
    {
        if (ctx.CurrentType?.Kind == TypeKind.Struct && HasAny(property.Modifiers, "virtual", "override"))
            Report(DiagnosticCode.TypeMismatch, $"Struct member '{property.Name}' cannot be virtual or override.", property.Location, ctx);

        if (!property.Modifiers.Contains("override", StringComparer.Ordinal))
            return;

        var exact = MemberSignatureHelper.Members(ctx.CurrentType?.Base, property.Name, MemberKind.Property)
            .FirstOrDefault(candidate => MemberSignatureHelper.SignatureExact(candidate, property));
        if (exact is null || !HasAny(exact.Modifiers, "virtual", "abstract"))
            Report(DiagnosticCode.MissingOverrideTarget, $"Property '{property.Name}' does not override a compatible virtual or abstract member.", property.Location, ctx);
        else if (exact.Modifiers.Contains("sealed", StringComparer.Ordinal))
            Report(DiagnosticCode.TypeMismatch, $"Property '{property.Name}' cannot override a sealed member.", property.Location, ctx);
    }

    private static bool HasAny(IReadOnlyCollection<string> modifiers, params string[] names) =>
        names.Any(name => modifiers.Contains(name, StringComparer.Ordinal));

    private static void Report(DiagnosticCode code, string message, SourceLocation location, VerificationContext ctx) =>
        ctx.Diagnostics.Report(code, DiagnosticSeverity.Error, message, location);
}
