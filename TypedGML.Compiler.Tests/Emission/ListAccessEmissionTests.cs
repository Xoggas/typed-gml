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
