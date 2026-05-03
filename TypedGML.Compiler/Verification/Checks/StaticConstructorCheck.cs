using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Declarations;
using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Diagnostics;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Verification.Checks;

public sealed class StaticConstructorCheck : ISemanticCheck
{
    public bool Matches(IAstNode node) => node is ClassDeclarationNode or StructDeclarationNode;

    public void Check(IAstNode node, VerificationContext ctx)
    {
        var members = node is ClassDeclarationNode @class ? @class.Members : ((StructDeclarationNode)node).Members;
        var staticConstructors = members.OfType<StaticConstructorDeclarationNode>().ToList();
        ReportDuplicateConstructors(staticConstructors, ctx);
        foreach (var staticConstructor in staticConstructors)
            CheckStaticConstructor(staticConstructor, ctx);
        foreach (var field in members.OfType<FieldDeclarationNode>())
            CheckStaticField(field, ctx);
    }

    private static void ReportDuplicateConstructors(IReadOnlyList<StaticConstructorDeclarationNode> staticConstructors, VerificationContext ctx)
    {
        foreach (var staticConstructor in staticConstructors.Skip(1))
            ctx.Diagnostics.Report(DiagnosticCode.DuplicateStaticConstructor, DiagnosticSeverity.Error, "A type may declare only one static constructor.", staticConstructor.Location);
    }

    private static void CheckStaticConstructor(StaticConstructorDeclarationNode staticConstructor, VerificationContext ctx)
    {
        if (ctx.CurrentType?.Members.Where(m => m.Kind == MemberKind.StaticConstructor).Any(m => m.Parameters.Count > 0) == true)
            ctx.Diagnostics.Report(DiagnosticCode.InvalidStaticConstructorUsage, DiagnosticSeverity.Error, "Static constructors cannot declare parameters.", staticConstructor.Location);

        StaticConstructorWalker.CheckBody(staticConstructor.Body, ctx);
    }

    private static void CheckStaticField(FieldDeclarationNode field, VerificationContext ctx)
    {
        if (!field.Modifiers.Contains("static", StringComparer.Ordinal) ||
            field.Initializer is null ||
            ConstantExpressionHelper.IsConstant(field.Initializer, ctx))
            return;

        StaticConstructorWalker.CheckInitializer(field.Initializer, ctx);
    }
}
