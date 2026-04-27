using TypedGML.Compiler.Tests.Infrastructure;

namespace TypedGML.Compiler.Tests;

public sealed class DeclarationDiagnosticTests
{
    [Fact]
    public void InterfaceMemberMustBeImplemented()
    {
        CompilerAssert.FailsWith(
            """
            public interface IShape {
                number Area();
            }

            public class Circle : IShape {
            }
            """,
            "TGML0006");
    }

    [Fact]
    public void OverrideRequiresVirtualOrAbstractAncestor()
    {
        CompilerAssert.FailsWith(
            """
            public class Base {
                public number Value() {
                    return 1;
                }
            }

            public class Child : Base {
                public override number Value() {
                    return 2;
                }
            }
            """,
            "TGML0007");
    }

    [Fact]
    public void SealedClassesCannotBeInherited()
    {
        CompilerAssert.FailsWith(
            """
            public sealed class Base {
            }

            public class Child : Base {
            }
            """,
            "TGML0005");
    }

    [Fact]
    public void StructMembersCannotBeVirtual()
    {
        CompilerAssert.FailsWith(
            """
            public struct Counter {
                public virtual number Value() {
                    return 1;
                }
            }
            """,
            "TGML0022");
    }

    [Fact]
    public void StaticIndexersAreRejected()
    {
        CompilerAssert.FailsWith(
            """
            public class Sample {
                public static number this[number index] {
                    get { return 1; }
                }
            }
            """,
            "TGML0036");
    }

    [Fact]
    public void DuplicateStaticConstructorsReportDiagnostic()
    {
        CompilerAssert.FailsWith(
            """
            public class Sample {
                static Sample() {
                }

                static Sample() {
                }
            }
            """,
            "TGML0043");
    }

    [Fact]
    public void MultipleObjectDecoratorsReportDiagnostic()
    {
        CompilerAssert.FailsWith(
            """
            using TypedGML.GameObjects;

            @Object("OBJ_One")
            @Object("OBJ_Two")
            public class Player : GameObject {
                public Player(number x, number y, string layer) {
                }
            }
            """,
            "TGML0024");
    }

    [Fact]
    public void GameObjectSubclassesRequireObjectDecorator()
    {
        CompilerAssert.FailsWith(
            """
            using TypedGML.GameObjects;

            public class Player : GameObject {
                public Player(number x, number y, string layer) {
                }
            }
            """,
            "TGML0045");
    }
}
