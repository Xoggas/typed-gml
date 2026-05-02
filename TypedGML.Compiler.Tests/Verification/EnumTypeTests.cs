using FluentAssertions;
using TypedGML.Compiler.Diagnostics;
using TypedGML.Compiler.Tests.Helpers;

namespace TypedGML.Compiler.Tests.Verification;

public sealed class EnumTypeTests
{
    [Fact]
    public void Test_EnumMemberAssignsToSameEnum_Valid() => Compile("""
        public enum EntityState { Idle = 0, Moving = 1, Dead = 2 }
        public class Actor {
            public EntityState State;
            public void Run() {
                var s = EntityState.Idle;
                EntityState copy = s;
                State = EntityState.Moving;
                switch (State) {
                    case EntityState.Dead: break;
                    default: break;
                }
            }
        }
        """).HasErrors.Should().BeFalse();

    [Fact]
    public void Test_EnumMemberNotAssignableToNumber_TGML0022() => Compile("""
        public enum EntityState { Idle = 0 }
        public class Actor {
            public void Run() {
                number n = EntityState.Idle;
            }
        }
        """).HasError(DiagnosticCode.TypeMismatch).Should().BeTrue();

    [Fact]
    public void Test_NumberNotAssignableToEnum_TGML0022() => Compile("""
        public enum EntityState { Idle = 0 }
        public class Actor {
            public void Run() {
                EntityState state = 0;
            }
        }
        """).HasError(DiagnosticCode.TypeMismatch).Should().BeTrue();

    [Fact]
    public void Test_DifferentEnumsAreNotAssignable_TGML0022() => Compile("""
        public enum EntityState { Idle = 0 }
        public enum OtherState { Idle = 0 }
        public class Actor {
            public void Run() {
                EntityState state = OtherState.Idle;
            }
        }
        """).HasError(DiagnosticCode.TypeMismatch).Should().BeTrue();

    private static CompileResult Compile(string source) =>
        CompilerFixture.Compile(source);
}
