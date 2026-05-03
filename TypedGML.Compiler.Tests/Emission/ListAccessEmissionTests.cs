using FluentAssertions;
using TypedGML.Compiler.Tests.Helpers;

namespace TypedGML.Compiler.Tests.Emission;

public sealed class ListAccessEmissionTests
{
    [Fact]
    public void Test_ListIndexerGet_EmitsFunction()
    {
        var result = Compile("""
            using TypedGML.Collections;

            public class ListAccessHost {
                public void Run(List<number> list, number i) {
                    var value = list[i];
                }
            }
            """);

        result.HasErrors.Should().BeFalse();
        var gml = result.GetFile("ListAccessHost.gml")!;

        GmlAssert.ContainsPattern(gml, "var value = TypedGML_Collections_List1_get_indexer(list, i);");
        GmlAssert.NotContainsPattern(gml, "list[i]");
    }

    [Fact]
    public void Test_ListMethodCall_EmitsFunction()
    {
        var result = Compile("""
            using TypedGML.Collections;

            public class ListAccessHost {
                public void Run(List<number> list, number x) {
                    list.Add(x);
                }
            }
            """);

        result.HasErrors.Should().BeFalse();
        var gml = result.GetFile("ListAccessHost.gml")!;

        GmlAssert.ContainsPattern(gml, "TypedGML_Collections_List1_Add(list, x);");
        GmlAssert.NotContainsPattern(gml, "list.Add(x)");
    }

    [Fact]
    public void Test_ListIndexerPropertyAccess_EmitsPropertyGetter()
    {
        var result = Compile("""
            using TypedGML.Collections;

            public class Entity {
                public bool IsAlive { get; set; }
                public number CurrentHp { get; set; }
            }

            public class ListAccessHost {
                public void Run(List<Entity> enemies, number i) {
                    var alive = enemies[i].IsAlive;
                    var hp = enemies[i].CurrentHp;
                }
            }
            """);

        result.HasErrors.Should().BeFalse(string.Join(Environment.NewLine, result.Errors.Select(error => error.Message)));
        var gml = result.GetFile("ListAccessHost.gml")!;

        GmlAssert.ContainsPattern(gml, "var alive = Entity_get_IsAlive(TypedGML_Collections_List1_get_indexer(enemies, i));");
        GmlAssert.ContainsPattern(gml, "var hp = Entity_get_CurrentHp(TypedGML_Collections_List1_get_indexer(enemies, i));");
        GmlAssert.NotContainsPattern(gml, "TypedGML_Collections_List1_get_indexer(enemies, i).IsAlive");
        GmlAssert.NotContainsPattern(gml, "TypedGML_Collections_List1_get_indexer(enemies, i).CurrentHp");
    }

    [Fact]
    public void Test_RawArrayIndexer_EmitsDirect()
    {
        var result = Compile("""
            public class ListAccessHost {
                public void Run() {
                    number[] arr = [1, 2, 3];
                    var value = arr[0];
                }
            }
            """);

        result.HasErrors.Should().BeFalse();
        var gml = result.GetFile("ListAccessHost.gml")!;

        GmlAssert.ContainsPattern(gml, "var value = arr[0];");
        GmlAssert.NotContainsPattern(gml, "get_indexer(arr, 0)");
    }

    private static CompileResult Compile(string source) => CompilerFixture.Compile(source);
}
