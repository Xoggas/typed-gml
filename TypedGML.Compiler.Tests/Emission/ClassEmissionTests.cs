using FluentAssertions;
using TypedGML.Compiler.Tests.Helpers;

namespace TypedGML.Compiler.Tests.Emission;

public sealed class ClassEmissionTests
{
    [Fact]
    public void Test_AbstractClassNoCreateFunction() =>
        GmlAssert.NotContainsPattern(Compile("public abstract class AbstractClass { public abstract void Run(); }").GetFile("AbstractClass.gml")!, "function AbstractClass_create");

    [Fact]
    public void Test_ConcreteClassHasCreateFunction() =>
        GmlAssert.HasFunction(Compile("public class ConcreteClass { }").GetFile("ConcreteClass.gml")!, "ConcreteClass_create");

    [Fact]
    public void Test_DefaultConstructorInitializesFields()
    {
        var result = Compile("""
            public class A {
                private string _value = "Hello, World!";
                private number _count;
            }
            """);

        result.HasErrors.Should().BeFalse();
        var gml = result.GetFile("A.gml")!;

        GmlAssert.ContainsPattern(gml, "self._value = \"Hello, World!\";");
        GmlAssert.ContainsPattern(gml, "self._count = 0;");
    }

    [Fact]
    public void Test_AutoProperty_DefaultBackingFieldSkippedWhenAssigned()
    {
        var result = Compile("""
            public class T {
                public number Value { get; set; }
                public string Name { get; }
                public bool Active { get; set; }

                public constructor(number value, bool active) {
                    Value = value;
                    Active = active;
                }
            }
            """);

        result.HasErrors.Should().BeFalse();
        var gml = result.GetFile("/T.gml")!;

        GmlAssert.NotContainsPattern(gml, "self.__backing_Value = 0;");
        GmlAssert.ContainsPattern(gml, "self.__backing_Name = undefined;");
        GmlAssert.NotContainsPattern(gml, "self.__backing_Active = false;");
        GmlAssert.ContainsPattern(gml, "T_set_Value(self, value);");
        GmlAssert.ContainsPattern(gml, "T_set_Active(self, active);");
    }

    [Fact]
    public void Test_ReadOnlyAutoProperty_ConstructorAssignsBackingField()
    {
        var result = Compile("""
            public class BankAccount {
                public string Owner { get; }

                public constructor(string owner) {
                    Owner = owner;
                }
            }
            """);

        result.HasErrors.Should().BeFalse();
        var gml = result.GetFile("BankAccount.gml")!;

        GmlAssert.HasFunction(gml, "BankAccount_get_Owner");
        GmlAssert.NotContainsPattern(gml, "BankAccount_set_Owner");
        GmlAssert.ContainsPattern(gml, "self.__backing_Owner = owner;");
    }

    [Fact]
    public void Test_OverrideInlined()
    {
        var gml = Compile("""
            public class Parent {
                public virtual number Value() { return 1; }
            }
            public class Child : Parent {
                public override number Value() { return 2; }
            }
            """).GetFile("Child.gml")!;
        var childMethod = FunctionBlock(gml, "function Child_Value");
        GmlAssert.ContainsPattern(childMethod, "return 2;");
        GmlAssert.NotContainsPattern(childMethod, "Parent_Value(");
    }

    [Fact]
    public void Test_BaseCallInlined()
    {
        var gml = Compile("""
            public class Parent {
                public virtual number Value() { var parent_local = 1; return parent_local; }
            }
            public class Child : Parent {
                public override number Value() { return base.Value(); }
            }
            """).GetFile("Child.gml")!;
        var childMethod = FunctionBlock(gml, "function Child_Value");
        GmlAssert.ContainsPattern(childMethod, "parent_local");
        GmlAssert.NotContainsPattern(childMethod, "Parent_Value(");
        GmlAssert.NotContainsPattern(childMethod, "return Value();");
    }

    [Fact]
    public void Test_DerivedClassDoesNotEmitInheritedMembers()
    {
        var result = Compile("""
            public class Animal {
                public virtual string Speak() { return "Animal"; }
            }
            public class Dog : Animal {
                public override string Speak() { return "Woof"; }
            }
            """);

        result.HasErrors.Should().BeFalse();
        var animal = result.GetFile("Animal.gml")!;
        var dog = result.GetFile("Dog.gml")!;

        GmlAssert.ContainsPattern(animal, "function Animal_create");
        GmlAssert.ContainsPattern(animal, "function Animal_Speak");
        GmlAssert.NotContainsPattern(animal, "function Dog_create");
        GmlAssert.NotContainsPattern(animal, "function Dog_Speak");

        GmlAssert.ContainsPattern(dog, "function Dog_create");
        GmlAssert.ContainsPattern(dog, "function Dog_Speak");
        GmlAssert.NotContainsPattern(dog, "function Animal_create");
        GmlAssert.NotContainsPattern(dog, "function Animal_Speak");

        Count(result.AllOutput(), "function Animal_create").Should().Be(1);
        Count(result.AllOutput(), "function Animal_Speak").Should().Be(1);
        Count(result.AllOutput(), "function Dog_create").Should().Be(1);
        Count(result.AllOutput(), "function Dog_Speak").Should().Be(1);
    }

    [Fact]
    public void Test_ConstructorSkipsDefaultInitForAssignedFields()
    {
        var result = Compile("""
            public class Animal {
                private string _name;
                public number Score;
                public constructor(string name) {
                    _name = name;
                }
            }
            """);

        result.HasErrors.Should().BeFalse();
        var gml = result.GetFile("Animal.gml")!;

        GmlAssert.NotContainsPattern(gml, "self._name = undefined;");
        GmlAssert.ContainsPattern(gml, "self._name = name;");
        GmlAssert.ContainsPattern(gml, "self.Score = 0;");
    }

    [Fact]
    public void Test_BaseConstructorChainInlinesParentBody()
    {
        var result = Compile("""
            public class Animal {
                public string Name;
                public constructor(string name) {
                    Name = name;
                }
            }
            public class Dog : Animal {
                public string Breed;
                public constructor(string name, string breed) : base(name) {
                    Breed = breed;
                }
            }
            """);

        result.HasErrors.Should().BeFalse();
        var dogCtor = FunctionBlock(result.GetFile("Dog.gml")!, "function Dog_create");

        GmlAssert.NotContainsPattern(dogCtor, "Animal_create(");
        GmlAssert.ContainsPattern(dogCtor, "self.Name = name;");
        GmlAssert.ContainsPattern(dogCtor, "self.Breed = breed;");
    }

    private static CompileResult Compile(string source) => CompilerFixture.Compile(source);

    private static string FunctionBlock(string gml, string prefix)
    {
        var start = gml.IndexOf(prefix, StringComparison.Ordinal);
        start.Should().BeGreaterThan(-1);
        var next = gml.IndexOf("\n\nfunction ", start + prefix.Length, StringComparison.Ordinal);
        return next < 0 ? gml[start..] : gml[start..next];
    }

    private static int Count(string text, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }
}
