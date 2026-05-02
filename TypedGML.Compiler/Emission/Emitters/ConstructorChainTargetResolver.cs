using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Declarations;
using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Emission.Emitters.Expressions;
using TypedGML.Compiler.Symbols;
using TypedGML.Compiler.Utils;

namespace TypedGML.Compiler.Emission.Emitters;

internal static class ConstructorChainTargetResolver
{
    public static ConstructorDeclarationNode? Resolve(
        TypeSymbol type,
        IReadOnlyList<IAstNode> args,
        EmitContext ctx,
        out IReadOnlyList<IAstNode> orderedArgs)
    {
        orderedArgs = args;
        if (!ctx.TypeDeclarations.TryGetValue(TypeDeclarationMapBuilder.Key(type), out var declaration) ||
            declaration is not ClassDeclarationNode @class)
            return null;

        var symbols = type.Members.Where(member => member.Kind == MemberKind.Constructor).ToList();
        var positionalArgs = CallArgumentOrderer.PositionalFromMixed(args);
        var namedArgs = CallArgumentOrderer.NamedFromMixed(args);
        var symbol = EmissionOverloadResolver.Pick(symbols, positionalArgs, namedArgs, ctx) ??
            symbols.FirstOrDefault(candidate => CallArgumentOrderer.TryOrder(candidate, positionalArgs, namedArgs, true, out _));

        if (symbol is null || !CallArgumentOrderer.TryOrder(symbol, positionalArgs, namedArgs, true, out orderedArgs))
            return null;

        return @class.Members.OfType<ConstructorDeclarationNode>()
            .FirstOrDefault(constructor => SignatureMatches(constructor, symbol));
    }

    private static bool SignatureMatches(ConstructorDeclarationNode constructor, MemberSymbol symbol) =>
        constructor.Parameters.Count == symbol.Parameters.Count &&
        constructor.Parameters.Zip(symbol.Parameters).All(pair => pair.First.TypeRef == pair.Second.TypeRef);
}
