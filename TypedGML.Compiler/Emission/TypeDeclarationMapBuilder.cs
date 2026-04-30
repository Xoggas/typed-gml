using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Declarations;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Emission;

internal static class TypeDeclarationMapBuilder
{
    public static IReadOnlyDictionary<string, IAstNode> Build(IReadOnlyList<FileNode> files)
    {
        var map = new Dictionary<string, IAstNode>(StringComparer.Ordinal);
        foreach (var file in files)
            Visit(file.TopLevelDeclarations, string.Empty, map);
        return map;
    }

    private static void Visit(IEnumerable<IAstNode> nodes, string currentNamespace, IDictionary<string, IAstNode> map)
    {
        foreach (var node in nodes)
        {
            switch (node)
            {
                case NamespaceDeclarationNode ns:
                    Visit(ns.Body, Combine(currentNamespace, ns.Name), map);
                    break;
                case ClassDeclarationNode type:
                    map[Key(Combine(currentNamespace, type.Name), type.GenericParams.Count)] = type;
                    break;
                case StructDeclarationNode type:
                    map[Key(Combine(currentNamespace, type.Name), type.GenericParams.Count)] = type;
                    break;
                case InterfaceDeclarationNode type:
                    map[Key(Combine(currentNamespace, type.Name), type.GenericParams.Count)] = type;
                    break;
            }
        }
    }

    public static string Key(TypeSymbol type) =>
        Key(type.QualifiedName, type.Arity);

    private static string Combine(string currentNamespace, string name) =>
        string.IsNullOrEmpty(currentNamespace) ? name : $"{currentNamespace}.{name}";

    private static string Key(string qualifiedName, int arity) =>
        arity == 0 ? qualifiedName : $"{qualifiedName}`{arity}";
}
