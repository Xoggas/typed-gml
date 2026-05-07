using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Diagnostics;
using TypedGML.Compiler.Symbols;
using TypedGML.Compiler.Utils;

namespace TypedGML.Compiler.Verification.Checks;

public sealed class ConstructorCallCheck : ISemanticCheck
{
    public bool Matches(IAstNode node) => node is ObjectCreationExpressionNode;

    public void Check(IAstNode node, VerificationContext ctx)
        => CheckCreation((ObjectCreationExpressionNode)node, ctx);

    private static void CheckCreation(ObjectCreationExpressionNode creation, VerificationContext ctx)
    {
        if (!SymbolResolver.TryResolveType(creation.TypeRef, creation.TypeArgs.Count, ctx, out var type))
            return;

        if (type.Kind == TypeKind.Interface || type.Kind == TypeKind.Enum)
            Report(DiagnosticCode.TypeMismatch, $"Type '{type.QualifiedName}' cannot be instantiated.", creation.Location, ctx);

        if (type.IsAbstract)
            Report(DiagnosticCode.AbstractClassInstantiation, $"Abstract type '{type.QualifiedName}' cannot be instantiated.", creation.Location, ctx);

        var constructors = type.Members.Where(member => member.Kind == MemberKind.Constructor).ToList();
        if (constructors.Count == 0)
        {
            if (creation.PositionalArgs.Count > 0 || creation.NamedArgs.Count > 0)
                Report(DiagnosticCode.NoMatchingMethodOverload, $"Type '{type.QualifiedName}' does not have a matching constructor.", creation.Location, ctx);
            return;
        }

        var substitutions = GenericTypeSubstitution.Map(type, creation.TypeArgs);
        var matches = constructors
            .Where(member => ConstructorSignatureMatcher.Matches(member, creation.PositionalArgs, creation.NamedArgs, substitutions, ctx))
            .ToList();
        if (matches.Count == 0)
        {
            Report(DiagnosticCode.NoMatchingMethodOverload, $"Type '{type.QualifiedName}' does not have a matching constructor.", creation.Location, ctx);
            return;
        }

        if (type.ObjectAssetName is not null &&
            !matches.Any(member => ObjectConstructorSpatialArguments.SuppliesRequiredValues(type, member, TryResolveSpatialConstructor)))
            Report(DiagnosticCode.InvalidObjectConstructorArgumentCount, "@Object construction requires x, y, and layer arguments from the constructor parameters or base constructor call.", creation.Location, ctx);
    }

    private static bool TryResolveSpatialConstructor(
        TypeSymbol type,
        IReadOnlyList<IAstNode> mixedArgs,
        out MemberSymbol? constructor,
        out IReadOnlyList<IAstNode> orderedArgs)
    {
        var positionalArgs = CallArgumentOrderer.PositionalFromMixed(mixedArgs);
        var namedArgs = CallArgumentOrderer.NamedFromMixed(mixedArgs);
        constructor = type.Members
            .Where(member => member.Kind == MemberKind.Constructor)
            .FirstOrDefault(member => CallArgumentOrderer.TryOrder(member, positionalArgs, namedArgs, true, out _));
        if (constructor is null)
        {
            orderedArgs = [];
            return false;
        }

        return CallArgumentOrderer.TryOrder(constructor, positionalArgs, namedArgs, true, out orderedArgs);
    }

    private static void Report(DiagnosticCode code, string message, SourceLocation location, VerificationContext ctx) =>
        ctx.Diagnostics.Report(code, DiagnosticSeverity.Error, message, location);
}
