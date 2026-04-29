using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Declarations;
using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Verification.Checks;

public sealed class AbstractCompletenessCheck : ISemanticCheck
{
    public bool Matches(IAstNode node) => node is ClassDeclarationNode;

    public void Check(IAstNode node, VerificationContext ctx)
    {
        var @class = (ClassDeclarationNode)node;
        var currentType = ctx.CurrentType;
        if (currentType is not null && ctx.Diagnostics.HasError(DiagnosticCode.SealedClassInheritance, currentType))
            return;

        CheckBodies(@class, ctx);
        if (ctx.CurrentType?.IsAbstract == false)
            CheckAbstractBaseMembers(@class, ctx);
    }

    private static void CheckAbstractBaseMembers(ClassDeclarationNode @class, VerificationContext ctx)
    {
        foreach (var member in AbstractMembers(ctx.CurrentType?.Base))
            if (!MemberSignatureHelper.Members(ctx.CurrentType, member.Name, member.Kind).Any(candidate => !candidate.Modifiers.Contains("abstract", StringComparer.Ordinal) && MemberSignatureHelper.SignatureExact(candidate, member)))
                Report(DiagnosticCode.AbstractMemberNotImplemented, $"Class '{@class.Name}' must implement abstract member '{member.Name}'.", @class.Location, ctx);
    }

    private static void CheckBodies(ClassDeclarationNode @class, VerificationContext ctx)
    {
        foreach (var method in @class.Members.OfType<MethodDeclarationNode>())
            if (method.Modifiers.Contains("abstract", StringComparer.Ordinal) ? method.Body is not null : method.Body is null)
                Report(DiagnosticCode.TypeMismatch, method.Modifiers.Contains("abstract", StringComparer.Ordinal) ? $"Abstract method '{method.Name}' cannot declare a body." : $"Method '{method.Name}' must declare a body.", method.Location, ctx);

        foreach (var property in @class.Members.OfType<PropertyDeclarationNode>().Where(property => property.Modifiers.Contains("abstract", StringComparer.Ordinal)))
            if (property.Accessors.Any(accessor => accessor.Body is not null))
                Report(DiagnosticCode.TypeMismatch, $"Abstract property '{property.Name}' cannot declare accessor bodies.", property.Location, ctx);
    }

    private static IEnumerable<Symbols.MemberSymbol> AbstractMembers(Symbols.TypeSymbol? type)
    {
        for (var current = type; current is not null; current = current.Base)
            foreach (var member in current.Members.Where(member => member.Modifiers.Contains("abstract", StringComparer.Ordinal)))
                yield return member;
    }

    private static void Report(DiagnosticCode code, string message, SourceLocation location, VerificationContext ctx) =>
        ctx.Diagnostics.Report(code, DiagnosticSeverity.Error, message, location);
}
