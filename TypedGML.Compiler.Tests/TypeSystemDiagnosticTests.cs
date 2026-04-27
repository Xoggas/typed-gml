using TypedGML.Compiler.Tests.Infrastructure;

namespace TypedGML.Compiler.Tests;

public sealed class TypeSystemDiagnosticTests
{
    [Fact]
    public void ConstFieldMustUseCompileTimeConstant()
    {
        CompilerAssert.FailsWith(
            """
            public class Sample {
                public const number Value = 1 + 2;
            }
            """,
            "TGML0010");
    }

    [Fact]
    public void DefaultParameterMustBeConstant()
    {
        CompilerAssert.FailsWith(
            """
            public class Sample {
                public number Value() { return 1; }

                public void Run(number x = Value()) {
                }
            }
            """,
            "TGML0015");
    }

    [Fact]
    public void GenericConstraintViolationReportsDiagnostic()
    {
        CompilerAssert.FailsWith(
            """
            public interface IToken {
                string Name();
            }

            public class Box<T : IToken> {
            }

            public class Sample {
                public Box<number> Make() {
                    return new Box<number>();
                }
            }
            """,
            "TGML0016");
    }

    [Fact]
    public void InvalidThrowTypeReportsDiagnostic()
    {
        CompilerAssert.FailsWith(
            """
            public class Sample {
                public void Run() {
                    throw 1;
                }
            }
            """,
            "TGML0017");
    }

    [Fact]
    public void SwitchCaseMustBeConstant()
    {
        CompilerAssert.FailsWith(
            """
            public class Sample {
                public void Run(number value) {
                    number label = 1;
                    switch (value) {
                        case label:
                            break;
                    }
                }
            }
            """,
            "TGML0018");
    }

    [Fact]
    public void NumberConditionsDoNotImplicitlyConvertToBool()
    {
        CompilerAssert.FailsWith(
            """
            public class Sample {
                public void Run() {
                    if (1) {
                    }
                }
            }
            """,
            "TGML0019");
    }

    [Fact]
    public void WithStatementRequiresObjectTypes()
    {
        CompilerAssert.FailsWith(
            """
            public class Plain {
            }

            public class Sample {
                public void Run(Plain value) {
                    with (value) {
                    }
                }
            }
            """,
            "TGML0011");
    }

    [Fact]
    public void EventAssignmentOutsideDeclaringTypeReportsDiagnostic()
    {
        CompilerAssert.FailsWith(
            """
            using TypedGML.Core;

            public class Counter {
                public event Action<number> OnChanged;
            }

            public class Other {
                public void Run(Counter counter) {
                    counter.OnChanged = null;
                }
            }
            """,
            "TGML0029");
    }
}
