using FluentAssertions;
using TypedGML.Compiler.Tests.Helpers;

namespace TypedGML.Compiler.Tests.Parser;

public sealed class ParserMemberTests
{
    [Fact]
    public void FieldModifierCombinationsParseSuccessfully() =>
        AssertParses("""
            public class FieldMembers {
                public number PublicValue;
                private static number StaticValue;
                protected readonly number ReadonlyValue;
                public const number ConstValue = 1;
                private static readonly number StaticReadonlyValue;
                protected static const number StaticConstValue = 2;
            }
            """);

    [Fact]
    public void AutoPropertyFormsParseSuccessfully() =>
        AssertParses("""
            public class AutoProperties {
                public number ReadWrite { get; set; }
                public number ReadOnly { get; }
                public number PrivateSet { get; private set; }
            }
            """);

    [Fact]
    public void PropertyBodiesParseSuccessfully() =>
        AssertParses("public class PropertyBodies { private number _value; public number Value { get { return _value; } set { _value = value; } } }");

    [Fact]
    public void MethodFormsParseSuccessfully() =>
        AssertParses("""
            public class MethodMembers {
                public T Identity<T>(T value) { return value; }
                public number Add(number x, number y = 1) { return x; }
                public number Use() { return Add(y: 2, x: 1); }
            }
            """);

    [Fact]
    public void ConstructorFormsParseSuccessfully() =>
        AssertParses("""
            public class ParentCtor { public ParentCtor(number value) { } }
            public class ChildCtor : ParentCtor {
                public ChildCtor() : base(1) { }
                public ChildCtor(number value) : this() { }
            }
            """);

    [Fact]
    public void StaticConstructorParsesSuccessfully() =>
        AssertParses("public class StaticCtorHost { public static number Value; static StaticCtorHost() { Value = 1; } }");

    [Fact]
    public void IndexerDeclarationParsesSuccessfully() =>
        AssertParses("public class IndexedMember { public number this[number index] { get { return index; } set { var local = value; } } }");

    [Fact]
    public void OperatorAndConversionDeclarationsParseSuccessfully() =>
        AssertParses("""
            public struct NumericMember {
                public number Value;
                public static NumericMember operator +(NumericMember a, NumericMember b) { return a; }
                public static implicit operator number(NumericMember value) { return value.Value; }
                public static explicit operator string(NumericMember value) { return "value"; }
            }
            """);

    [Fact]
    public void EventDeclarationParsesSuccessfully() =>
        AssertParses("using TypedGML; public class EventMember { public event Action Changed; }");

    [Fact]
    public void DecoratorsParseSuccessfully() =>
        AssertParses("@ParserClass public class DecoratedMember { @ParserMethod public void Run() { } @ParserProperty public number Value { get; set; } }");

    private static void AssertParses(string source)
    {
        var result = CompilerFixture.Compile(source);
        result.HasErrors.Should().BeFalse(string.Join(Environment.NewLine, result.Errors.Select(error => error.Message)));
    }
}
