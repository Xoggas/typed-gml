using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Declarations;
using TypedGML.Compiler.Ast.Expressions;
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
                    PopulateMembers(type.Members, Combine(currentNamespace, type.Name), type.GenericParams.Count);
                    VisitNodes(type.Members, currentNamespace);
                    break;
                case StructDeclarationNode type:
                    PopulateMembers(type.Members, Combine(currentNamespace, type.Name), type.GenericParams.Count);
                    VisitNodes(type.Members, currentNamespace);
                    break;
                case InterfaceDeclarationNode type:
                    PopulateMembers(type.Members, Combine(currentNamespace, type.Name), type.GenericParams.Count);
                    break;
                case EnumDeclarationNode type:
                    PopulateEnumMembers(type.Members, Combine(currentNamespace, type.Name));
                    break;
                case DelegateDeclarationNode type:
                    PopulateDelegate(type, Combine(currentNamespace, type.Name), type.GenericParams.Count);
                    break;
            }
        }
    }

    private void PopulateMembers(IEnumerable<IAstNode> members, string qualifiedTypeName, int arity)
    {
        if (!symbolTable.TryResolve(qualifiedTypeName, arity, null, [], out var typeSymbol))
            return;

        foreach (var member in members)
        {
            var symbol = member switch
            {
                FieldDeclarationNode field => new MemberSymbol { Name = field.Name, Kind = MemberKind.Field, ReturnType = field.TypeRef, Parameters = [], Modifiers = field.Modifiers.ToHashSet(StringComparer.Ordinal) },
                PropertyDeclarationNode property => new MemberSymbol
                {
                    Name = property.Name,
                    Kind = MemberKind.Property,
                    ReturnType = property.TypeRef,
                    Parameters = [],
                    Modifiers = property.Modifiers.ToHashSet(StringComparer.Ordinal),
                    NativePropertyName = DecoratorArg(property.Decorators, "NativeProperty"),
                    AssetName = DecoratorArg(property.Decorators, "Asset")
                },
                MethodDeclarationNode method => new MemberSymbol { Name = method.Name, Kind = MemberKind.Method, ReturnType = method.TypeRef, Parameters = Parameters(method.Parameters), GenericParameters = method.GenericParams, Modifiers = method.Modifiers.ToHashSet(StringComparer.Ordinal), NativeEventName = DecoratorArg(method.Decorators, "NativeEvent"), NativeCallName = DecoratorArg(method.Decorators, "NativeCall") },
                ConstructorDeclarationNode ctor => new MemberSymbol { Name = ".ctor", Kind = MemberKind.Constructor, ReturnType = "void", Parameters = Parameters(ctor.Parameters), Modifiers = ctor.Modifiers.ToHashSet(StringComparer.Ordinal), ConstructorChainTarget = ctor.ChainTarget, ConstructorChainArgs = ctor.ChainArgs },
                StaticConstructorDeclarationNode ctor => new MemberSymbol { Name = ".cctor", Kind = MemberKind.StaticConstructor, ReturnType = "void", Parameters = Parameters(ctor.Parameters), Modifiers = new HashSet<string>(StringComparer.Ordinal) { "static" } },
                IndexerDeclarationNode indexer => new MemberSymbol { Name = "this", Kind = MemberKind.Indexer, ReturnType = indexer.TypeRef, Parameters = [Parameter(indexer.Parameter)], Modifiers = indexer.Modifiers.ToHashSet(StringComparer.Ordinal) },
                OperatorDeclarationNode op => new MemberSymbol { Name = op.OperatorSymbol, Kind = MemberKind.Operator, ReturnType = op.ReturnType, Parameters = Parameters(op.Parameters), Modifiers = op.Modifiers.ToHashSet(StringComparer.Ordinal), NativeCallName = DecoratorArg(op.Decorators, "NativeCall") },
                ConversionOperatorNode op => new MemberSymbol { Name = op.ConversionKind.ToString(), Kind = MemberKind.ConversionOperator, ReturnType = op.TargetType, Parameters = [Parameter(op.Parameter)], Modifiers = op.Modifiers.ToHashSet(StringComparer.Ordinal), NativeCallName = DecoratorArg(op.Decorators, "NativeCall") },
                EventDeclarationNode evt => new MemberSymbol { Name = evt.Name, Kind = MemberKind.Event, ReturnType = evt.TypeRef, Parameters = [], Modifiers = evt.Modifiers.ToHashSet(StringComparer.Ordinal) },
                _ => null
            };

            if (symbol is not null)
                typeSymbol.Members.Add(symbol);
        }

        PopulateOverloads(typeSymbol, MemberKind.Method);
        PopulateOverloads(typeSymbol, MemberKind.Constructor);
    }

    private static void PopulateOverloads(TypeSymbol typeSymbol, MemberKind kind)
    {
        foreach (var group in typeSymbol.Members.Where(m => m.Kind == kind).GroupBy(m => m.Name, StringComparer.Ordinal))
        {
            var members = group.ToList();
            foreach (var member in members)
                member.Overloads.AddRange(members.Where(other => !ReferenceEquals(other, member)));
        }
    }

    private static IReadOnlyList<ParameterSymbol> Parameters(IEnumerable<ParameterNode> parameters) =>
        parameters.Select(Parameter).ToList();

    private static ParameterSymbol Parameter(ParameterNode parameter) =>
        new(parameter.Name, parameter.TypeRef, parameter.DefaultValue is not null, parameter.DefaultValue);

    private static string? DecoratorArg(IEnumerable<DecoratorNode> decorators, string name)
    {
        var decorator = decorators.FirstOrDefault(d => string.Equals(d.Name, name, StringComparison.Ordinal));
        return decorator?.Args.FirstOrDefault() is LiteralExpressionNode literal
            ? literal.Value?.ToString()
            : null;
    }

    private void PopulateEnumMembers(IEnumerable<EnumMemberNode> members, string qualifiedTypeName)
    {
        if (!symbolTable.TryResolve(qualifiedTypeName, null, [], out var typeSymbol))
            return;

        foreach (var member in members)
            typeSymbol.Members.Add(new MemberSymbol
            {
                Name = member.Name,
                Kind = MemberKind.Field,
                ReturnType = qualifiedTypeName,
                Parameters = [],
                Modifiers = new HashSet<string>(StringComparer.Ordinal) { "const" }
            });
    }

    private void PopulateDelegate(DelegateDeclarationNode declaration, string qualifiedTypeName, int arity)
    {
        if (!symbolTable.TryResolve(qualifiedTypeName, arity, null, [], out var typeSymbol))
            return;

        typeSymbol.Members.Add(new MemberSymbol
        {
            Name = "Invoke",
            Kind = MemberKind.Method,
            ReturnType = declaration.ReturnType,
            Parameters = Parameters(declaration.Parameters),
            Modifiers = declaration.Modifiers.ToHashSet(StringComparer.Ordinal)
        });
    }

    private static string Combine(string currentNamespace, string name) =>
        string.IsNullOrEmpty(currentNamespace) ? name : $"{currentNamespace}.{name}";
}
