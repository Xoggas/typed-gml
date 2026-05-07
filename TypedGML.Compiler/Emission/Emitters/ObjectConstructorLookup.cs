using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Emission.Emitters.Expressions;
using TypedGML.Compiler.Symbols;
using TypedGML.Compiler.Utils;

namespace TypedGML.Compiler.Emission.Emitters;

internal static class ObjectConstructorLookup
{
    public static ConstructorLookup Create(EmitContext ctx) =>
        (TypeSymbol type, IReadOnlyList<IAstNode> args, out MemberSymbol? symbol, out IReadOnlyList<IAstNode> orderedArgs) =>
            TryResolve(type, args, ctx, out symbol, out orderedArgs);

    private static bool TryResolve(
        TypeSymbol type,
        IReadOnlyList<IAstNode> args,
        EmitContext ctx,
        out MemberSymbol? symbol,
        out IReadOnlyList<IAstNode> orderedArgs)
    {
        var positionalArgs = CallArgumentOrderer.PositionalFromMixed(args);
        var namedArgs = CallArgumentOrderer.NamedFromMixed(args);
        var constructors = type.Members.Where(member => member.Kind == MemberKind.Constructor).ToList();
        symbol = EmissionOverloadResolver.Pick(constructors, positionalArgs, namedArgs, ctx) ??
            constructors.FirstOrDefault(candidate => CallArgumentOrderer.TryOrder(candidate, positionalArgs, namedArgs, true, out _));
        if (symbol is null)
        {
            orderedArgs = [];
            return false;
        }

        return CallArgumentOrderer.TryOrder(symbol, positionalArgs, namedArgs, true, out orderedArgs);
    }
}
