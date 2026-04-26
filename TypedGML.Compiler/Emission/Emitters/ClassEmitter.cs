using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Declarations;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Emission.Emitters;

public sealed class ClassEmitter : INodeEmitter
{
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

    private static void EmitScript(ClassDeclarationNode declaration, EmitContext ctx)
    {
        foreach (var member in declaration.Members.OfType<FieldDeclarationNode>())
            ctx.Dispatch(member, ctx);

        var constructors = declaration.Members.OfType<ConstructorDeclarationNode>().ToList();
        if (constructors.Count == 0 && ctx.CurrentType is not null)
            EmitDefaultConstructor(ctx);
        foreach (var constructor in constructors)
            ctx.Dispatch(constructor, ctx);
        foreach (var member in declaration.Members.Where(m => m is not FieldDeclarationNode and not ConstructorDeclarationNode))
            ctx.Dispatch(member, ctx);
    }

    private static void EmitGameObject(ClassDeclarationNode declaration, EmitContext ctx)
    {
        foreach (var member in declaration.Members.OfType<FieldDeclarationNode>())
            ctx.Dispatch(member, ctx);
        foreach (var constructor in declaration.Members.OfType<ConstructorDeclarationNode>())
            EmitObjectConstructor(ctx, constructor);
        foreach (var method in declaration.Members.OfType<MethodDeclarationNode>())
        {
            var eventName = DecoratorArg(method.Decorators, "NativeEvent");
            if (eventName is not null)
                GmlEventMap.Resolve(eventName);
            ctx.Dispatch(method, ctx);
        }
        foreach (var member in declaration.Members.Where(m => m is not FieldDeclarationNode and not ConstructorDeclarationNode and not MethodDeclarationNode))
            ctx.Dispatch(member, ctx);
    }

    private static void EmitDefaultConstructor(EmitContext ctx)
    {
        ctx.Writer.Write($"function {NamingConvention.ConstructorName(ctx.CurrentType!)}()");
        ctx.Writer.BeginBlock();
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

    private static string? DecoratorArg(IReadOnlyList<DecoratorNode> decorators, string name) =>
        decorators.FirstOrDefault(d => d.Name == name)?.Args.FirstOrDefault() is LiteralExpressionNode literal
            ? literal.Value?.ToString()
            : null;
}
