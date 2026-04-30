using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Diagnostics;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Verification.Checks;

public sealed class ConstructorCallCheck : ISemanticCheck
{
    public bool Matches(IAstNode node) => node is ConstructorDeclarationNode or ObjectCreationExpressionNode;

    public void Check(IAstNode node, VerificationContext ctx)
    {
        if (node is ConstructorDeclarationNode constructor)
            CheckChain(constructor, ctx);
        else
            CheckCreation((ObjectCreationExpressionNode)node, ctx);
    }

    private static void CheckChain(ConstructorDeclarationNode constructor, VerificationContext ctx)
    {
        var owner = constructor.ChainTarget == ConstructorChainTarget.Base ? ctx.CurrentType?.Base : ctx.CurrentType;
        if (constructor.ChainTarget == ConstructorChainTarget.None || owner is null)
            return;

        var matches = owner.Members.Where(member => member.Kind == MemberKind.Constructor)
            .Where(member => Matches(member, constructor.ChainArgs, ctx)).ToList();
        if (matches.Count == 0)
            Report(DiagnosticCode.NoMatchingMethodOverload, "Constructor chain target does not have a matching constructor.", constructor.Location, ctx);

        if (constructor.ChainTarget == ConstructorChainTarget.This &&
            matches.Any(match => MemberSignatureHelper.ParametersExact(match.Parameters, constructor.Parameters)))
            Report(DiagnosticCode.TypeMismatch, "Constructor chaining cannot target the same constructor recursively.", constructor.Location, ctx);
    }

    private static void CheckCreation(ObjectCreationExpressionNode creation, VerificationContext ctx)
    {
        if (!SymbolResolver.TryResolveType(creation.TypeRef, creation.TypeArgs.Count, ctx, out var type))
            return;

        if (type.Kind == TypeKind.Interface || type.Kind == TypeKind.Enum)
            Report(DiagnosticCode.TypeMismatch, $"Type '{type.QualifiedName}' cannot be instantiated.", creation.Location, ctx);

        if (type.IsAbstract)
            Report(DiagnosticCode.AbstractClassInstantiation, $"Abstract type '{type.QualifiedName}' cannot be instantiated.", creation.Location, ctx);

        var positionalArgs = creation.PositionalArgs;
        if (type.ObjectAssetName is not null && creation.PositionalArgs.Count < 3)
            Report(DiagnosticCode.InvalidObjectConstructorArgumentCount, "@Object construction requires x, y, and layer arguments.", creation.Location, ctx);

        var constructors = type.Members.Where(member => member.Kind == MemberKind.Constructor).ToList();
        if (constructors.Count == 0)
        {
            if (positionalArgs.Count > 0 || creation.NamedArgs.Count > 0)
                Report(DiagnosticCode.NoMatchingMethodOverload, $"Type '{type.QualifiedName}' does not have a matching constructor.", creation.Location, ctx);
            return;
        }

        var substitutions = GenericTypeSubstitution.Map(type, creation.TypeArgs);
        if (!constructors.Any(member => Matches(member, positionalArgs, substitutions, ctx)))
            Report(DiagnosticCode.NoMatchingMethodOverload, $"Type '{type.QualifiedName}' does not have a matching constructor.", creation.Location, ctx);
    }

    private static bool Matches(MemberSymbol constructor, IReadOnlyList<IAstNode> args, VerificationContext ctx) =>
        Matches(constructor, args, new Dictionary<string, string>(StringComparer.Ordinal), ctx);

    private static bool Matches(MemberSymbol constructor, IReadOnlyList<IAstNode> args, IReadOnlyDictionary<string, string> substitutions, VerificationContext ctx)
    {
        if (args.Count > constructor.Parameters.Count || args.Count < MemberSignatureHelper.RequiredParameters(constructor))
            return false;

        for (var i = 0; i < args.Count; i++)
            if (!TypeReferenceHelper.IsAssignable(GenericTypeSubstitution.Substitute(constructor.Parameters[i].TypeRef, substitutions), ExpressionTypeResolver.Resolve(args[i], ctx), ctx))
                return false;

        return true;
    }

    private static void Report(DiagnosticCode code, string message, SourceLocation location, VerificationContext ctx) =>
        ctx.Diagnostics.Report(code, DiagnosticSeverity.Error, message, location);
}
