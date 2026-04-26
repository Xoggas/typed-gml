using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Declarations;
using TypedGML.Compiler.Diagnostics;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Population;

public sealed class InheritanceResolver(SymbolTable symbolTable, DiagnosticBag diagnostics)
{
    internal SymbolTable SymbolTable => symbolTable;

    internal DiagnosticBag Diagnostics => diagnostics;

    public void Populate(IReadOnlyList<FileNode> files)
    {
        foreach (var file in files)
            ResolveNodes(file.TopLevelDeclarations, string.Empty);

        DetectCycles();
    }

    private void ResolveNodes(IEnumerable<IAstNode> nodes, string currentNamespace)
    {
        foreach (var node in nodes)
        {
            switch (node)
            {
                case NamespaceDeclarationNode ns:
                    ResolveNodes(ns.Body, Combine(currentNamespace, ns.Name));
                    break;
                case ClassDeclarationNode type:
                    ResolveType(type.BaseTypes, Combine(currentNamespace, type.Name), type.Location, false);
                    ResolveNodes(type.Members, currentNamespace);
                    break;
                case StructDeclarationNode type:
                    ResolveType(type.BaseTypes, Combine(currentNamespace, type.Name), type.Location, true);
                    ResolveNodes(type.Members, currentNamespace);
                    break;
                case InterfaceDeclarationNode type:
                    ResolveInterfaces(type.BaseTypes, Combine(currentNamespace, type.Name));
                    break;
            }
        }
    }

    private void ResolveType(IReadOnlyList<string> baseTypes, string qualifiedName, Diagnostics.SourceLocation location, bool isStruct)
    {
        if (!symbolTable.TryResolve(qualifiedName, null, [], out var typeSymbol))
            return;

        if (!symbolTable.TryResolve("object", null, [], out var objectType))
            objectType = null!;

        typeSymbol.Base = isStruct || typeSymbol.Kind == TypeKind.Class ? objectType : null;
        typeSymbol.Interfaces.Clear();
        for (var i = 0; i < baseTypes.Count; i++)
        {
            if (!symbolTable.TryResolve(baseTypes[i], null, [], out var resolved))
                continue;

            if (isStruct)
            {
                if (resolved.Kind != TypeKind.Interface)
                {
                    diagnostics.Report(DiagnosticCode.InvalidStructInheritance, DiagnosticSeverity.Error, $"Struct '{qualifiedName}' cannot inherit from '{baseTypes[i]}'.", location);
                    continue;
                }

                typeSymbol.Interfaces.Add(resolved);
                continue;
            }

            if (i == 0 && resolved.Kind != TypeKind.Interface)
                typeSymbol.Base = resolved;
            else
                typeSymbol.Interfaces.Add(resolved);
        }
    }

    private void ResolveInterfaces(IReadOnlyList<string> baseTypes, string qualifiedName)
    {
        if (!symbolTable.TryResolve(qualifiedName, null, [], out var typeSymbol))
            return;

        typeSymbol.Interfaces.Clear();
        foreach (var baseType in baseTypes)
            if (symbolTable.TryResolve(baseType, null, [], out var resolved))
                typeSymbol.Interfaces.Add(resolved);
    }

    private void DetectCycles()
    {
        var states = new Dictionary<TypeSymbol, int>();
        foreach (var type in symbolTable.AllTypes)
            Visit(type, states);
    }

    private void Visit(TypeSymbol type, Dictionary<TypeSymbol, int> states)
    {
        if (states.TryGetValue(type, out var state))
        {
            if (state == 1)
                diagnostics.Report(DiagnosticCode.CircularInheritance, DiagnosticSeverity.Error, $"Circular inheritance detected for '{type.QualifiedName}'.", new Diagnostics.SourceLocation(string.Empty, 0, 0));
            return;
        }

        states[type] = 1;
        if (type.Base is not null)
            Visit(type.Base, states);
        foreach (var @interface in type.Interfaces)
            Visit(@interface, states);
        states[type] = 2;
    }

    private static string Combine(string currentNamespace, string name) =>
        string.IsNullOrEmpty(currentNamespace) ? name : $"{currentNamespace}.{name}";
}
