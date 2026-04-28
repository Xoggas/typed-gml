using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Diagnostics;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Verification.Checks;

public sealed class ObjectCreationCheck : ISemanticCheck
{
    public bool Matches(IAstNode node) => node is ObjectCreationExpressionNode;

    public void Check(IAstNode node, VerificationContext ctx)
    {
        var creation = (ObjectCreationExpressionNode)node;
        if (!SymbolResolver.TryResolveType(creation.TypeRef, ctx, out var type))
            return;

        if (type.Kind == TypeKind.Interface || type.Kind == TypeKind.Enum)
            Report($"Type '{type.QualifiedName}' cannot be created with new.", DiagnosticCode.TypeMismatch, creation.Location, ctx);

        if (type.IsAbstract)
            Report($"Abstract type '{type.QualifiedName}' cannot be instantiated.", DiagnosticCode.AbstractClassInstantiation, creation.Location, ctx);

        var args = creation.PositionalArgs;
        var substitutions = GenericTypeSubstitution.Map(type, creation.TypeArgs);
        if (type.Members.Any(member => member.Kind == MemberKind.Constructor) &&
            !type.Members.Where(member => member.Kind == MemberKind.Constructor).Any(member => Matches(member, args, substitutions, ctx)))
            Report($"Type '{type.QualifiedName}' does not have a matching constructor.", DiagnosticCode.NoMatchingMethodOverload, creation.Location, ctx);
    }

    private static bool Matches(MemberSymbol ctor, IReadOnlyList<IAstNode> args, IReadOnlyDictionary<string, string> substitutions, VerificationContext ctx) =>
        args.Count <= ctor.Parameters.Count &&
        args.Count >= MemberSignatureHelper.RequiredParameters(ctor) &&
        args.Select((arg, i) => TypeReferenceHelper.IsAssignable(Substitute(ctor.Parameters[i].TypeRef, substitutions), ExpressionTypeResolver.Resolve(arg, ctx), ctx)).All(x => x);

    private static string Substitute(string typeRef, IReadOnlyDictionary<string, string> substitutions) =>
        GenericTypeSubstitution.Substitute(typeRef, substitutions);

    private static void Report(string message, DiagnosticCode code, SourceLocation location, VerificationContext ctx) =>
        ctx.Diagnostics.Report(code, DiagnosticSeverity.Error, message, location);
}
