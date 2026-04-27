using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Declarations;
using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Emission.Emitters;

public sealed class ClassEmitter(StaticCtorEmitter staticCtorEmitter) : INodeEmitter
{
    private readonly ClassEventEmitter _eventEmitter = new();

    public bool Matches(IAstNode node) => node is ClassDeclarationNode;

    public void Emit(IAstNode node, EmitContext ctx)
    {
        var declaration = (ClassDeclarationNode)node;
        var previousType = ctx.CurrentType;
        ctx.CurrentType = ResolveType(ctx, declaration.Name);
        if (ctx.Decorators.ObjectAssetName is not null)
            EmitGameObject(declaration, ctx);
        else
            EmitScript(declaration, ctx);
        ctx.CurrentType = previousType;
    }

    private void EmitScript(ClassDeclarationNode declaration, EmitContext ctx)
    {
        foreach (var member in declaration.Members.OfType<FieldDeclarationNode>())
            ctx.Dispatch(member, ctx);

        if (ctx.CurrentType?.IsAbstract == true)
        {
            staticCtorEmitter.EmitStaticCtor(ctx.CurrentType, declaration.Members, ctx);
            return;
        }

        var constructors = declaration.Members.OfType<ConstructorDeclarationNode>().ToList();
        if (constructors.Count == 0 && ctx.CurrentType is not null && !ctx.CurrentType.IsAbstract)
            EmitDefaultConstructor(ctx);
        foreach (var constructor in constructors)
            ctx.Dispatch(constructor, ctx);
        foreach (var member in declaration.Members.Where(m => m is not FieldDeclarationNode and not ConstructorDeclarationNode))
            ctx.Dispatch(member, ctx);
        foreach (var member in InheritedConcreteMembers(declaration, ctx))
            ctx.Dispatch(member, ctx);
        if (ctx.CurrentType is not null)
            staticCtorEmitter.EmitStaticCtor(ctx.CurrentType, declaration.Members, ctx);
    }

    private void EmitGameObject(ClassDeclarationNode declaration, EmitContext ctx)
    {
        foreach (var member in declaration.Members.OfType<FieldDeclarationNode>())
            ctx.Dispatch(member, ctx);
        foreach (var constructor in declaration.Members.OfType<ConstructorDeclarationNode>())
            EmitObjectConstructor(ctx, constructor);
        foreach (var method in declaration.Members.OfType<MethodDeclarationNode>().Where(m => !m.Modifiers.Contains("static", StringComparer.Ordinal)))
        {
            var eventName = _eventEmitter.ResolveEventName(method, ctx.CurrentType);
            if (eventName is not null)
                _eventEmitter.Emit(declaration, method, eventName, ctx);
            else
                ctx.Dispatch(method, ctx);
        }
        foreach (var member in declaration.Members.Where(m => m is not FieldDeclarationNode and not ConstructorDeclarationNode and not MethodDeclarationNode))
            ctx.Dispatch(member, ctx);
        if (ctx.CurrentType is not null)
            staticCtorEmitter.EmitStaticCtor(ctx.CurrentType, declaration.Members, ctx);
    }

    private static void EmitDefaultConstructor(EmitContext ctx)
    {
        ctx.Writer.Write($"function {NamingConvention.ConstructorName(ctx.CurrentType!)}()");
        ctx.Writer.BeginBlock();
        ctx.Writer.WriteLine("var self = {};");
        ctx.Writer.WriteLine("return self;");
        ctx.Writer.EndBlock();
    }

    private static void EmitObjectConstructor(EmitContext ctx, ConstructorDeclarationNode constructor)
    {
        var parameters = string.Join(", ", constructor.Parameters.Select(p => p.Name));
        var objectName = ctx.CurrentType?.ObjectAssetName ?? ctx.Decorators.ObjectAssetName ?? NamingConvention.TypeName(ctx.CurrentType!);
        var x = constructor.Parameters.ElementAtOrDefault(0)?.Name ?? "x";
        var y = constructor.Parameters.ElementAtOrDefault(1)?.Name ?? "y";
        var layer = constructor.Parameters.ElementAtOrDefault(2)?.Name ?? "layer";
        ctx.Writer.Write($"function {NamingConvention.ConstructorName(ctx.CurrentType!)}({parameters})");
        ctx.Writer.BeginBlock();
        if (constructor.Parameters.Count <= 3)
            ctx.Writer.WriteLine($"return instance_create_layer({x}, {y}, {layer}, {objectName});");
        else
        {
            ctx.Writer.WriteLine($"var __inst = instance_create_layer({x}, {y}, {layer}, {objectName});");
            ctx.Writer.WriteLine("with (__inst)");
            ctx.Writer.BeginBlock();
            ctx.Dispatch(constructor.Body, ctx);
            ctx.Writer.EndBlock();
            ctx.Writer.WriteLine("return __inst;");
        }
        ctx.Writer.EndBlock();
    }

    private static TypeSymbol? ResolveType(EmitContext ctx, string name) =>
        ctx.Symbols.TryResolve(name, ctx.CurrentNamespacePrefix, [], out var symbol) ? symbol : null;

    private static IEnumerable<IAstNode> InheritedConcreteMembers(ClassDeclarationNode declaration, EmitContext ctx)
    {
        var declared = new HashSet<string>(declaration.Members.OfType<MethodDeclarationNode>().Select(Signature), StringComparer.Ordinal);
        foreach (var current in BaseDeclarations(ctx.CurrentType, ctx))
            foreach (var method in current.Members.OfType<MethodDeclarationNode>())
            {
                var signature = Signature(method);
                if (method.Modifiers.Contains("abstract", StringComparer.Ordinal) ||
                    method.Modifiers.Contains("static", StringComparer.Ordinal) ||
                    !declared.Add(signature))
                    continue;
                yield return method;
            }
    }

    private static IEnumerable<ClassDeclarationNode> BaseDeclarations(TypeSymbol? type, EmitContext ctx)
    {
        for (var current = type?.Base; current is not null; current = current.Base)
            if (ctx.TypeDeclarations.TryGetValue(current.QualifiedName, out var declaration) && declaration is ClassDeclarationNode @class)
                yield return @class;
    }

    private static string Signature(MethodDeclarationNode method) =>
        $"{method.Name}({string.Join(",", method.Parameters.Select(p => p.TypeRef))})";
}
