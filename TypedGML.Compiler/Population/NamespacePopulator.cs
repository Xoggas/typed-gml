using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Declarations;
using TypedGML.Compiler.Diagnostics;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Population;

public sealed class NamespacePopulator(SymbolTable symbolTable, DiagnosticBag diagnostics)
{
    private readonly Dictionary<string, Dictionary<string, string>> _usingMaps = new(StringComparer.Ordinal);
    private readonly HashSet<string> _namespaces = new(StringComparer.Ordinal);

    internal SymbolTable SymbolTable => symbolTable;

    internal DiagnosticBag Diagnostics => diagnostics;

    public void Populate(IReadOnlyList<FileNode> files)
    {
        foreach (var file in files)
        {
            CollectNamespaces(file.TopLevelDeclarations, string.Empty);
            var usingMap = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var usingDirective in file.TopLevelDeclarations.OfType<UsingDirectiveNode>())
            {
                var key = usingDirective.Alias ?? LastSegment(usingDirective.QualifiedName);
                usingMap[key] = usingDirective.QualifiedName;
            }

            _usingMaps[file.FilePath] = usingMap;
        }
    }

    public IReadOnlyDictionary<string, string> GetUsingMap(string filePath) =>
        _usingMaps.TryGetValue(filePath, out var usingMap)
            ? usingMap
            : new Dictionary<string, string>();

    public bool ContainsNamespace(string qualifiedName) =>
        _namespaces.Contains(qualifiedName);

    private void CollectNamespaces(IEnumerable<IAstNode> nodes, string currentNamespace)
    {
        foreach (var ns in nodes.OfType<NamespaceDeclarationNode>())
        {
            var qualifiedName = Combine(currentNamespace, ns.Name);
            if (symbolTable.TryResolve(qualifiedName, null, [], out _))
                diagnostics.Report(
                    DiagnosticCode.NamespaceTypeNameConflict,
                    DiagnosticSeverity.Error,
                    $"Namespace '{qualifiedName}' conflicts with an existing type name.",
                    ns.Location);

            _namespaces.Add(qualifiedName);
            CollectNamespaces(ns.Body, qualifiedName);
        }
    }

    private static string LastSegment(string qualifiedName)
    {
        var index = qualifiedName.LastIndexOf('.');
        return index >= 0 ? qualifiedName[(index + 1)..] : qualifiedName;
    }

    private static string Combine(string currentNamespace, string name) =>
        string.IsNullOrEmpty(currentNamespace) ? name : $"{currentNamespace}.{name}";
}
