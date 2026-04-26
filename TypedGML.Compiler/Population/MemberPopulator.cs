using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Declarations;
using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Diagnostics;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Population;

public sealed class MemberPopulator(SymbolTable symbolTable, DiagnosticBag diagnostics)
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
                    PopulateMembers(type.Members, Combine(currentNamespace, type.Name));
                    VisitNodes(type.Members, currentNamespace);
                    break;
                case StructDeclarationNode type:
                    PopulateMembers(type.Members, Combine(currentNamespace, type.Name));
                    VisitNodes(type.Members, currentNamespace);
                    break;
                case InterfaceDeclarationNode type:
                    PopulateMembers(type.Members, Combine(currentNamespace, type.Name));
                    break;
            }
        }
    }

    private void PopulateMembers(IEnumerable<IAstNode> members, string qualifiedTypeName)
    {
        if (!symbolTable.TryResolve(qualifiedTypeName, null, [], out var typeSymbol))
            return;

        foreach (var member in members)
        {
            var symbol = member switch
            {
                FieldDeclarationNode field => new MemberSymbol { Name = field.Name, Kind = MemberKind.Field, ReturnType = field.TypeRef, Parameters = [], Modifiers = field.Modifiers.ToHashSet(StringComparer.Ordinal) },
                PropertyDeclarationNode property => new MemberSymbol { Name = property.Name, Kind = MemberKind.Property, ReturnType = property.TypeRef, Parameters = [], Modifiers = property.Modifiers.ToHashSet(StringComparer.Ordinal) },
                MethodDeclarationNode method => new MemberSymbol { Name = method.Name, Kind = MemberKind.Method, ReturnType = method.TypeRef, Parameters = Parameters(method.Parameters), Modifiers = method.Modifiers.ToHashSet(StringComparer.Ordinal) },
                ConstructorDeclarationNode ctor => new MemberSymbol { Name = ".ctor", Kind = MemberKind.Constructor, ReturnType = "void", Parameters = Parameters(ctor.Parameters), Modifiers = ctor.Modifiers.ToHashSet(StringComparer.Ordinal) },
                IndexerDeclarationNode indexer => new MemberSymbol { Name = "this", Kind = MemberKind.Indexer, ReturnType = indexer.TypeRef, Parameters = [Parameter(indexer.Parameter)], Modifiers = indexer.Modifiers.ToHashSet(StringComparer.Ordinal) },
                OperatorDeclarationNode op => new MemberSymbol { Name = op.OperatorSymbol, Kind = MemberKind.Operator, ReturnType = op.ReturnType, Parameters = Parameters(op.Parameters), Modifiers = op.Modifiers.ToHashSet(StringComparer.Ordinal) },
                ConversionOperatorNode op => new MemberSymbol { Name = op.ConversionKind.ToString(), Kind = MemberKind.ConversionOperator, ReturnType = op.TargetType, Parameters = [Parameter(op.Parameter)], Modifiers = op.Modifiers.ToHashSet(StringComparer.Ordinal) },
                EventDeclarationNode evt => new MemberSymbol { Name = evt.Name, Kind = MemberKind.Event, ReturnType = evt.TypeRef, Parameters = [], Modifiers = evt.Modifiers.ToHashSet(StringComparer.Ordinal) },
                _ => null
            };

            if (symbol is not null)
                typeSymbol.Members.Add(symbol);
        }

        foreach (var group in typeSymbol.Members.Where(m => m.Kind == MemberKind.Method).GroupBy(m => m.Name, StringComparer.Ordinal))
        {
            var methods = group.ToList();
            foreach (var method in methods)
                method.Overloads.AddRange(methods.Where(other => !ReferenceEquals(other, method)));
        }
    }

    private static IReadOnlyList<ParameterSymbol> Parameters(IEnumerable<ParameterNode> parameters) =>
        parameters.Select(Parameter).ToList();

    private static ParameterSymbol Parameter(ParameterNode parameter) =>
        new(parameter.Name, parameter.TypeRef, parameter.DefaultValue is not null, parameter.DefaultValue);

    private static string Combine(string currentNamespace, string name) =>
        string.IsNullOrEmpty(currentNamespace) ? name : $"{currentNamespace}.{name}";
}
