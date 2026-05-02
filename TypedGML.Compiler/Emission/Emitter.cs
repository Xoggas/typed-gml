using System.Diagnostics;
using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Declarations;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Diagnostics;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Emission;

public sealed class Emitter(
    IReadOnlyList<INodeEmitter> emitters,
    DecoratorProcessor decoratorProcessor,
    FileOrganizer fileOrganizer,
    SymbolTable symbolTable,
    DiagnosticBag diagnostics,
    IGmlOutputSink? outputSink = null)
{
    private IReadOnlyDictionary<string, IAstNode> _typeDeclarations = new Dictionary<string, IAstNode>(StringComparer.Ordinal);

    public void Emit(IReadOnlyList<FileNode> files)
    {
        AssertNoOverlap();
        _typeDeclarations = TypeDeclarationMapBuilder.Build(files);
        foreach (var file in files)
        {
            var ctx = NewContext();
            ctx.UsingPrefixes = file.TopLevelDeclarations.OfType<UsingDirectiveNode>()
                .Select(u => u.QualifiedName).ToList();
            foreach (var node in file.TopLevelDeclarations)
                EmitNode(node, ctx.WithWriter(new GmlWriter()), persist: true);
        }
    }

    internal void Dispatch(IAstNode node, EmitContext ctx)
    {
        var matches = emitters.Where(emitter => emitter.Matches(node)).ToList();
        Debug.Assert(matches.Count <= 1, $"Multiple emitters match {node.GetType().Name}.");
        if (matches.Count == 0)
            return;

        try
        {
            matches[0].Emit(node, ctx);
        }
        catch (InvalidOperationException ex) when (ex.Message.StartsWith("TGML0035:", StringComparison.Ordinal))
        {
            ctx.Diagnostics.Report(DiagnosticCode.UnknownNativeEventName, DiagnosticSeverity.Error, ex.Message["TGML0035: ".Length..], node.Location);
        }
    }

    private void AssertNoOverlap()
    {
        Debug.Assert(OverlapCount<FileNode>() <= 1);
        Debug.Assert(OverlapCount<NamespaceDeclarationNode>() <= 1);
        Debug.Assert(OverlapCount<ClassDeclarationNode>() <= 1);
        Debug.Assert(OverlapCount<StructDeclarationNode>() <= 1);
        Debug.Assert(OverlapCount<InterfaceDeclarationNode>() <= 1);
        Debug.Assert(OverlapCount<EnumDeclarationNode>() <= 1);
        Debug.Assert(OverlapCount<DelegateDeclarationNode>() <= 1);
        Debug.Assert(OverlapCount<FunctionDeclarationNode>() <= 1);
        Debug.Assert(OverlapCount<FieldDeclarationNode>() <= 1);
        Debug.Assert(OverlapCount<PropertyDeclarationNode>() <= 1);
        Debug.Assert(OverlapCount<MethodDeclarationNode>() <= 1);
        Debug.Assert(OverlapCount<ConstructorDeclarationNode>() <= 1);
        Debug.Assert(OverlapCount<IndexerDeclarationNode>() <= 1);
        Debug.Assert(OverlapCount<OperatorDeclarationNode>() <= 1);
        Debug.Assert(OverlapCount<ConversionOperatorNode>() <= 1);
        Debug.Assert(OverlapCount<EventDeclarationNode>() <= 1);
    }

    private int OverlapCount<T>() where T : IAstNode
    {
        var sample = Sample<T>();
        return sample is null ? 0 : emitters.Count(emitter => emitter.Matches(sample));
    }

    private void EmitNode(IAstNode node, EmitContext ctx, bool persist)
    {
        if (node is NamespaceDeclarationNode ns)
        {
            var nsPrefix = Combine(ctx.CurrentNamespacePrefix, ns.Name);
            foreach (var child in ns.Body)
            {
                var childCtx = ctx.WithWriter(new GmlWriter());
                childCtx.CurrentNamespacePrefix = nsPrefix;
                EmitNode(child, childCtx, persist);
            }
            return;
        }

        var previousDecorators = ctx.Decorators;
        ctx.Decorators = Decorators(node);
        Dispatch(node, ctx);
        if (persist)
            EmitterPersistence.Persist(node, ctx, fileOrganizer, symbolTable);
        ctx.Decorators = previousDecorators;
    }

    private static IAstNode? Sample<T>() where T : IAstNode =>
        typeof(T) == typeof(FileNode) ? new FileNode(string.Empty, [], new SourceLocation(string.Empty, 0, 0)) :
        typeof(T) == typeof(NamespaceDeclarationNode) ? new NamespaceDeclarationNode(string.Empty, [], false, null, new SourceLocation(string.Empty, 0, 0)) :
        typeof(T) == typeof(ClassDeclarationNode) ? new ClassDeclarationNode(string.Empty, [], [], [], [], [], null, new SourceLocation(string.Empty, 0, 0)) :
        typeof(T) == typeof(StructDeclarationNode) ? new StructDeclarationNode(string.Empty, [], [], [], [], [], null, new SourceLocation(string.Empty, 0, 0)) :
        typeof(T) == typeof(InterfaceDeclarationNode) ? new InterfaceDeclarationNode(string.Empty, [], [], [], [], null, new SourceLocation(string.Empty, 0, 0)) :
        typeof(T) == typeof(EnumDeclarationNode) ? new EnumDeclarationNode(string.Empty, [], [], null, new SourceLocation(string.Empty, 0, 0)) :
        typeof(T) == typeof(DelegateDeclarationNode) ? new DelegateDeclarationNode(string.Empty, [], [], string.Empty, [], [], null, new SourceLocation(string.Empty, 0, 0)) :
        typeof(T) == typeof(FunctionDeclarationNode) ? new FunctionDeclarationNode(string.Empty, [], [], string.Empty, [], [], new FileNode(string.Empty, [], new SourceLocation(string.Empty, 0, 0)), null, new SourceLocation(string.Empty, 0, 0)) :
        typeof(T) == typeof(FieldDeclarationNode) ? new FieldDeclarationNode(string.Empty, string.Empty, [], [], null, null, new SourceLocation(string.Empty, 0, 0)) :
        typeof(T) == typeof(PropertyDeclarationNode) ? new PropertyDeclarationNode(string.Empty, string.Empty, [], [], [], null, new SourceLocation(string.Empty, 0, 0)) :
        typeof(T) == typeof(MethodDeclarationNode) ? new MethodDeclarationNode(string.Empty, string.Empty, [], [], [], [], null, null, new SourceLocation(string.Empty, 0, 0)) :
        typeof(T) == typeof(ConstructorDeclarationNode) ? new ConstructorDeclarationNode([], [], [], ConstructorChainTarget.None, [], new FileNode(string.Empty, [], new SourceLocation(string.Empty, 0, 0)), null, new SourceLocation(string.Empty, 0, 0)) :
        typeof(T) == typeof(IndexerDeclarationNode) ? new IndexerDeclarationNode(string.Empty, [], new ParameterNode(string.Empty, string.Empty, null, [], new SourceLocation(string.Empty, 0, 0)), [], [], null, new SourceLocation(string.Empty, 0, 0)) :
        typeof(T) == typeof(OperatorDeclarationNode) ? new OperatorDeclarationNode(string.Empty, [], [], string.Empty, [], null, null, new SourceLocation(string.Empty, 0, 0)) :
        typeof(T) == typeof(ConversionOperatorNode) ? new ConversionOperatorNode(ConversionOperatorKind.Implicit, string.Empty, new ParameterNode(string.Empty, string.Empty, null, [], new SourceLocation(string.Empty, 0, 0)), [], [], null, null, new SourceLocation(string.Empty, 0, 0)) :
        typeof(T) == typeof(EventDeclarationNode) ? new EventDeclarationNode(string.Empty, string.Empty, [], [], null, new SourceLocation(string.Empty, 0, 0)) :
        null;

    private DecoratorAnnotations Decorators(IAstNode node) => node switch
    {
        ClassDeclarationNode n => decoratorProcessor.Process(n.Decorators, diagnostics),
        StructDeclarationNode n => decoratorProcessor.Process(n.Decorators, diagnostics),
        InterfaceDeclarationNode n => decoratorProcessor.Process(n.Decorators, diagnostics),
        EnumDeclarationNode n => decoratorProcessor.Process(n.Decorators, diagnostics),
        DelegateDeclarationNode n => decoratorProcessor.Process(n.Decorators, diagnostics),
        FunctionDeclarationNode n => decoratorProcessor.Process(n.Decorators, diagnostics),
        FieldDeclarationNode n => decoratorProcessor.Process(n.Decorators, diagnostics),
        PropertyDeclarationNode n => decoratorProcessor.Process(n.Decorators, diagnostics),
        IndexerDeclarationNode n => decoratorProcessor.Process(n.Decorators, diagnostics),
        MethodDeclarationNode n => decoratorProcessor.Process(n.Decorators, diagnostics),
        ConstructorDeclarationNode n => decoratorProcessor.Process(n.Decorators, diagnostics),
        OperatorDeclarationNode n => decoratorProcessor.Process(n.Decorators, diagnostics),
        ConversionOperatorNode n => decoratorProcessor.Process(n.Decorators, diagnostics),
        EventDeclarationNode n => decoratorProcessor.Process(n.Decorators, diagnostics),
        _ => new DecoratorAnnotations(null, null, null, null, null, null)
    };

    private static string Combine(string? currentNamespace, string name) =>
        string.IsNullOrEmpty(currentNamespace) ? name : $"{currentNamespace}.{name}";

    private EmitContext NewContext() =>
        new(symbolTable, new GmlWriter(), fileOrganizer, outputSink ?? new FileGmlOutputSink(), new DecoratorAnnotations(null, null, null, null, null, null), diagnostics, Dispatch)
        {
            TypeDeclarations = _typeDeclarations
        };
}
