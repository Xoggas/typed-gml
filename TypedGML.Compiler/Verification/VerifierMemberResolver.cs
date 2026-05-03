using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Verification;

internal static class VerifierMemberResolver
{
    public static MemberSymbol FindMethod(MethodDeclarationNode method, VerificationContext ctx) =>
        SymbolResolver.FindMember(ctx.CurrentType, method.Name, out _) ?? new MemberSymbol { Name = method.Name, ReturnType = method.TypeRef, Parameters = Parameters(method.Parameters), GenericParameters = method.GenericParams, Modifiers = Modifiers(method.Modifiers) };

    public static MemberSymbol FindConstructor(ConstructorDeclarationNode ctor, VerificationContext ctx) =>
        ctx.CurrentType?.Members.FirstOrDefault(m => m.Kind == MemberKind.Constructor && ParametersMatch(m.Parameters, ctor.Parameters)) ?? new MemberSymbol { Name = ".ctor", ReturnType = "void", Parameters = Parameters(ctor.Parameters), Modifiers = Modifiers(ctor.Modifiers) };

    public static MemberSymbol FindStaticConstructor(StaticConstructorDeclarationNode ctor, VerificationContext ctx) =>
        ctx.CurrentType?.Members.FirstOrDefault(m => m.Kind == MemberKind.StaticConstructor) ?? new MemberSymbol { Name = ".cctor", Kind = MemberKind.StaticConstructor, ReturnType = "void", Parameters = Parameters(ctor.Parameters), Modifiers = new HashSet<string>(StringComparer.Ordinal) { "static" } };

    public static MemberSymbol ResolveProperty(PropertyDeclarationNode property) =>
        new() { Name = property.Name, Kind = MemberKind.Property, ReturnType = property.TypeRef, Modifiers = Modifiers(property.Modifiers) };

    public static MemberSymbol ResolveIndexer(IndexerDeclarationNode indexer) =>
        new() { Name = "this", Kind = MemberKind.Indexer, ReturnType = indexer.TypeRef, Parameters = [new ParameterSymbol(indexer.Parameter.Name, indexer.Parameter.TypeRef, indexer.Parameter.DefaultValue is not null, indexer.Parameter.DefaultValue)], Modifiers = Modifiers(indexer.Modifiers) };

    private static IReadOnlyList<ParameterSymbol> Parameters(IEnumerable<ParameterNode> parameters) =>
        parameters.Select(p => new ParameterSymbol(p.Name, p.TypeRef, p.DefaultValue is not null, p.DefaultValue)).ToList();

    private static HashSet<string> Modifiers(IEnumerable<string> modifiers) =>
        modifiers.ToHashSet(StringComparer.Ordinal);

    private static bool ParametersMatch(IReadOnlyList<ParameterSymbol> symbols, IReadOnlyList<ParameterNode> parameters) =>
        symbols.Count == parameters.Count && symbols.Zip(parameters).All(p => p.First.TypeRef == p.Second.TypeRef);
}
