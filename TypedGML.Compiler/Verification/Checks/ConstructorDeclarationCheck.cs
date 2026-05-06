using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Declarations;
using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Diagnostics;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Verification.Checks;

public sealed class ConstructorDeclarationCheck : ISemanticCheck
{
    public bool Matches(IAstNode node) => node is ClassDeclarationNode or ConstructorDeclarationNode;

    public void Check(IAstNode node, VerificationContext ctx)
    {
        if (node is ClassDeclarationNode declaration)
            CheckClass(declaration, ctx);
        else
            CheckConstructor((ConstructorDeclarationNode)node, ctx);
    }

    private static void CheckClass(ClassDeclarationNode declaration, VerificationContext ctx)
    {
        if (!TryGetBaseType(ctx, out var baseType) ||
            declaration.Members.OfType<ConstructorDeclarationNode>().Any())
            return;

        if (!ConstructorSignatureMatcher.HasParameterless(baseType, ctx))
            Report(declaration.Location, ctx);
    }

    private static void CheckConstructor(ConstructorDeclarationNode constructor, VerificationContext ctx)
    {
        if (!TryGetBaseType(ctx, out var baseType))
            return;

        if (constructor.ChainTarget == ConstructorChainTarget.Base)
        {
            if (!ConstructorSignatureMatcher.Matches(baseType, constructor.ChainArgs, ctx))
                Report(constructor.Location, ctx);
            return;
        }

        if (constructor.ChainTarget == ConstructorChainTarget.This)
        {
            CheckThisChain(constructor, ctx);
            return;
        }

        if (!ConstructorSignatureMatcher.HasParameterless(baseType, ctx))
            Report(constructor.Location, ctx);
    }

    private static void CheckThisChain(ConstructorDeclarationNode constructor, VerificationContext ctx)
    {
        var matches = ctx.CurrentType?.Members
            .Where(member => member.Kind == MemberKind.Constructor)
            .Where(member => ConstructorSignatureMatcher.Matches(member, constructor.ChainArgs, ctx))
            .ToList() ?? [];

        if (matches.Count == 0)
            ctx.Diagnostics.Report(
                DiagnosticCode.NoMatchingMethodOverload,
                DiagnosticSeverity.Error,
                "Constructor chain target does not have a matching constructor.",
                constructor.Location);

        if (matches.Any(match => MemberSignatureHelper.ParametersExact(match.Parameters, constructor.Parameters)))
            ctx.Diagnostics.Report(
                DiagnosticCode.TypeMismatch,
                DiagnosticSeverity.Error,
                "Constructor chaining cannot target the same constructor recursively.",
                constructor.Location);
    }

    private static bool TryGetBaseType(VerificationContext ctx, out TypeSymbol baseType)
    {
        baseType = ctx.CurrentType?.Base!;
        return ctx.CurrentType?.Kind == TypeKind.Class && baseType is not null;
    }

    private static void Report(SourceLocation location, VerificationContext ctx) =>
        ctx.Diagnostics.Report(
            DiagnosticCode.NoMatchingMethodOverload,
            DiagnosticSeverity.Error,
            "Constructor does not chain to a matching base constructor.",
            location);
}
