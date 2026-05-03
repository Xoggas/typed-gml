using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Declarations;
using TypedGML.Compiler.Diagnostics;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Population;

public sealed class TypePopulator(
    SymbolTable symbolTable,
    DiagnosticBag diagnostics,
    NamespacePopulator namespacePopulator)
{
    internal SymbolTable SymbolTable => symbolTable;

    internal DiagnosticBag Diagnostics => diagnostics;

    public void Populate(IReadOnlyList<FileNode> files)
    {
        foreach (var file in files)
            VisitNodes(file.TopLevelDeclarations, string.Empty);
    }

    private void VisitNodes(IEnumerable<IAstNode> nodes, string currentNamespace)
    {
        foreach (var node in nodes)
        {
            switch (node)
            {
                case NamespaceDeclarationNode ns:
                    VisitNodes(ns.Body, Combine(currentNamespace, ns.Name));
                    break;
                case ClassDeclarationNode type:
                    Register(type.Name, currentNamespace, TypeKind.Class, type.GenericParams, type.Modifiers, type.Decorators);
                    VisitNodes(type.Members, currentNamespace);
                    break;
                case StructDeclarationNode type:
                    Register(type.Name, currentNamespace, TypeKind.Struct, type.GenericParams, type.Modifiers, type.Decorators);
                    VisitNodes(type.Members, currentNamespace);
                    break;
                case InterfaceDeclarationNode type:
                    Register(type.Name, currentNamespace, TypeKind.Interface, type.GenericParams, [], type.Decorators);
                    VisitNodes(type.Members, currentNamespace);
                    break;
                case EnumDeclarationNode type:
                    Register(type.Name, currentNamespace, TypeKind.Enum, [], [], type.Decorators);
                    break;
                case DelegateDeclarationNode type:
                    Register(type.Name, currentNamespace, TypeKind.Delegate, type.GenericParams, type.Modifiers, type.Decorators);
                    break;
                case FunctionDeclarationNode function:
                    Register(function.Name, currentNamespace, TypeKind.Delegate, function.GenericParams, function.Modifiers, function.Decorators);
                    break;
            }
        }
    }

    private void Register(
        string name,
        string currentNamespace,
        TypeKind kind,
        IReadOnlyList<Ast.Members.GenericParamNode> genericParameters,
        IReadOnlyList<string> modifiers,
        IReadOnlyList<Ast.Members.DecoratorNode> decorators)
    {
        var symbol = new TypeSymbol
        {
            QualifiedName = Combine(currentNamespace, name),
            Kind = kind,
            IsAbstract = modifiers.Contains("abstract", StringComparer.Ordinal),
            IsSealed = modifiers.Contains("sealed", StringComparer.Ordinal),
            ObjectAssetName = decorators.FirstOrDefault(d => d.Name == "Object")
                is { Args.Count: > 0 } decorator
                    ? (decorator.Args[0] as Ast.Expressions.LiteralExpressionNode)?.Value?.ToString()
                    : null
        };

        if (namespacePopulator.ContainsNamespace(symbol.QualifiedName))
        {
            diagnostics.Report(
                DiagnosticCode.NamespaceTypeNameConflict,
                DiagnosticSeverity.Error,
                $"Type '{symbol.QualifiedName}' conflicts with an existing namespace.",
                new SourceLocation(string.Empty, 0, 0));
            return;
        }

        symbol.GenericParameters.AddRange(genericParameters);
        symbolTable.Register(symbol.QualifiedName, symbol);
    }

    private static string Combine(string currentNamespace, string name) =>
        string.IsNullOrEmpty(currentNamespace) ? name : $"{currentNamespace}.{name}";
}
