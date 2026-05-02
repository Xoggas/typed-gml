using FluentAssertions;
using TypedGML.Compiler.Diagnostics;
using TypedGML.Compiler.Tests.Helpers;

namespace TypedGML.Compiler.Tests.Verification;

public sealed class CollisionDecoratorTests
{
    [Fact]
    public void Test_CollisionArgumentMustBeTypeof_TGML0047() =>
        AssertHasError("""
            using TypedGML.GameObjects;
            @Object("OBJ_Player")
            public class Player : GameObject {
                @Collision("Enemy")
                public void OnCollisionEnemy() { }
            }
            """, DiagnosticCode.CollisionArgumentMustBeTypeof);

    [Fact]
    public void Test_CollisionUnknownType_TGML0022() =>
        AssertHasError("""
            using TypedGML.GameObjects;
            @Object("OBJ_Player")
            public class Player : GameObject {
                @Collision(typeof(Enemy))
                public void OnCollisionEnemy() { }
            }
            """, DiagnosticCode.TypeMismatch);

    [Fact]
    public void Test_CollisionTargetMustBeObject_TGML0048() =>
        AssertHasError("""
            using TypedGML.GameObjects;
            public class Enemy { }
            @Object("OBJ_Player")
            public class Player : GameObject {
                @Collision(typeof(Enemy))
                public void OnCollisionEnemy() { }
            }
            """, DiagnosticCode.CollisionTargetMissingObjectDecorator);

    [Fact]
    public void Test_CollisionOnlyOnObjectMethods_TGML0049() =>
        AssertHasError("""
            @Object("OBJ_Enemy")
            public class Enemy { }
            public class Player {
                @Collision(typeof(Enemy))
                public void OnCollisionEnemy() { }
            }
            """, DiagnosticCode.CollisionDecoratorInvalidTarget);

    private static void AssertHasError(string source, DiagnosticCode code) =>
        CompilerFixture.Compile(source).HasError(code).Should().BeTrue();
}
