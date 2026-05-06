using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Symbols;
using TypedGML.Compiler.Utils;

namespace TypedGML.Compiler.Verification.Checks;

internal static class ConstructorSignatureMatcher
{
    public static bool HasParameterless(TypeSymbol type, VerificationContext ctx) =>
        Matches(type, [], ctx);

    public static bool Matches(TypeSymbol type, IReadOnlyList<IAstNode> mixedArgs, VerificationContext ctx)
    {
        var constructors = type.Members.Where(member => member.Kind == MemberKind.Constructor).ToList();
        if (constructors.Count == 0)
            return mixedArgs.Count == 0;

        return constructors.Any(member => Matches(member, mixedArgs, ctx));
    }

    public static bool Matches(MemberSymbol constructor, IReadOnlyList<IAstNode> mixedArgs, VerificationContext ctx) =>
        Matches(
            constructor,
            CallArgumentOrderer.PositionalFromMixed(mixedArgs),
            CallArgumentOrderer.NamedFromMixed(mixedArgs),
            new Dictionary<string, string>(StringComparer.Ordinal),
            ctx);

    public static bool Matches(
        MemberSymbol constructor,
        IReadOnlyList<IAstNode> positionalArgs,
        IReadOnlyList<NamedArgNode> namedArgs,
        IReadOnlyDictionary<string, string> substitutions,
        VerificationContext ctx)
    {
        if (!CallArgumentOrderer.TryBind(constructor, positionalArgs, namedArgs, out var bindings))
            return false;

        foreach (var binding in bindings)
        {
            var targetType = GenericTypeSubstitution.Substitute(constructor.Parameters[binding.ParameterIndex].TypeRef, substitutions);
            if (!TypeReferenceHelper.IsAssignable(targetType, ExpressionTypeResolver.Resolve(binding.Value, ctx), ctx))
                return false;
        }

        return true;
    }
}
