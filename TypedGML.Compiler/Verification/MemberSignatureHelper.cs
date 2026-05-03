using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Verification;

internal static class MemberSignatureHelper
{
    public static IEnumerable<MemberSymbol> Members(TypeSymbol? type, string name, MemberKind? kind = null)
    {
        for (var current = type; current is not null; current = current.Base)
            foreach (var member in current.Members)
                if (member.Name == name && (kind is null || member.Kind == kind))
                    yield return member;
    }

    public static int RequiredParameters(MemberSymbol member) =>
        member.Parameters.Count(parameter => !parameter.HasDefault);

    public static bool ParametersExact(IReadOnlyList<ParameterSymbol> left, IReadOnlyList<ParameterSymbol> right) =>
        left.Count == right.Count && left.Zip(right).All(pair => pair.First.TypeRef == pair.Second.TypeRef);

    public static bool ParametersExact(IReadOnlyList<ParameterSymbol> left, IReadOnlyList<ParameterNode> right) =>
        left.Count == right.Count && left.Zip(right).All(pair => pair.First.TypeRef == pair.Second.TypeRef);

    public static bool SignatureExact(MemberSymbol left, MemberSymbol right) =>
        left.Kind == right.Kind &&
        left.Name == right.Name &&
        left.ReturnType == right.ReturnType &&
        ParametersExact(left.Parameters, right.Parameters);

    public static bool SignatureExact(MemberSymbol symbol, MethodDeclarationNode method) =>
        symbol.Kind == MemberKind.Method &&
        symbol.Name == method.Name &&
        symbol.ReturnType == method.TypeRef &&
        ParametersExact(symbol.Parameters, method.Parameters);

    public static bool SignatureExact(MemberSymbol symbol, PropertyDeclarationNode property) =>
        symbol.Kind == MemberKind.Property &&
        symbol.Name == property.Name &&
        symbol.ReturnType == property.TypeRef;
}
