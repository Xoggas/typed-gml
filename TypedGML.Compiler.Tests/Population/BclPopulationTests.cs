using FluentAssertions;
using TypedGML.Compiler.Symbols;
using TypedGML.Compiler.Tests.Helpers;

namespace TypedGML.Compiler.Tests.Population;

public sealed class BclPopulationTests
{
    [Fact]
    public void Test_PrimitiveBclOperatorsRegistered()
    {
        var (result, symbols) = PopulationFixture.CompileWithSymbols("public class UserType { }");

        result.HasErrors.Should().BeFalse();

        var number = Resolve(symbols, "TypedGML.Core.Number");
        var @string = Resolve(symbols, "TypedGML.Core.String");
        var @bool = Resolve(symbols, "TypedGML.Core.Bool");

        AssertOperator(number, "+", "number", "__op_add", "number", "number");
        AssertOperator(number, "-", "number", "__op_sub", "number", "number");
        AssertOperator(number, "*", "number", "__op_mul", "number", "number");
        AssertOperator(number, "/", "number", "__op_div", "number", "number");
        AssertOperator(number, "%", "number", "__op_mod", "number", "number");
        AssertOperator(number, "<", "bool", "__op_lt", "number", "number");
        AssertOperator(number, "+", "string", "__op_str_num_add", "string", "number");
        AssertOperator(number, "+", "string", "__op_num_str_add", "number", "string");
        AssertOperator(@string, "+", "string", "__op_add", "string", "string");
        AssertOperator(@bool, "==", "bool", "__op_eq", "bool", "bool");
    }

    private static TypeSymbol Resolve(SymbolTable symbols, string qualifiedName)
    {
        symbols.TryResolve(qualifiedName, null, [], out var type).Should().BeTrue();
        return type;
    }

    private static void AssertOperator(
        TypeSymbol type,
        string name,
        string returnType,
        string nativeCallName,
        params string[] parameterTypes)
    {
        var member = type.Members.Single(m =>
            m.Kind == MemberKind.Operator &&
            m.Name == name &&
            m.Parameters.Select(p => p.TypeRef).SequenceEqual(parameterTypes, StringComparer.Ordinal));

        member.ReturnType.Should().Be(returnType);
        member.NativeCallName.Should().Be(nativeCallName);
    }
}
