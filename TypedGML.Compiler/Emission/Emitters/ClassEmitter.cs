using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Declarations;
using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Emission.Emitters;

public sealed class ClassEmitter(StaticCtorEmitter staticCtorEmitter) : INodeEmitter
{
    private readonly ClassEventEmitter _eventEmitter = new();
    private readonly ObjectConstructorEmitter _objectConstructorEmitter = new();

    public bool Matches(IAstNode node) => node is ClassDeclarationNode;

    public void Emit(IAstNode node, EmitContext ctx)
    {
        var declaration = (ClassDeclarationNode)node;
        var previousType = ctx.CurrentType;
        ctx.CurrentType = ResolveType(ctx, declaration.Name, declaration.GenericParams.Count);
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
        if (ctx.CurrentType is not null)
            staticCtorEmitter.EmitStaticCtor(ctx.CurrentType, declaration.Members, ctx);
    }

    private void EmitGameObject(ClassDeclarationNode declaration, EmitContext ctx)
    {
        foreach (var member in declaration.Members.OfType<FieldDeclarationNode>())
            ctx.Dispatch(member, ctx);
        foreach (var constructor in declaration.Members.OfType<ConstructorDeclarationNode>())
            _objectConstructorEmitter.Emit(ctx, constructor);
        var createEventEmitted = false;
        foreach (var method in declaration.Members.OfType<MethodDeclarationNode>().Where(m => !m.Modifiers.Contains("static", StringComparer.Ordinal)))
        {
            var collisionTargetFileName = _eventEmitter.ResolveCollisionTargetFileName(method, ctx);
            if (collisionTargetFileName is not null)
            {
                _eventEmitter.EmitCollision(declaration, method, collisionTargetFileName, ctx);
                continue;
            }

            var eventName = _eventEmitter.ResolveEventName(method, ctx.CurrentType);
            if (eventName is not null)
            {
                createEventEmitted |= string.Equals(eventName, "Create", StringComparison.Ordinal);
                _eventEmitter.Emit(declaration, method, eventName, ctx);
            }
            else
                ctx.Dispatch(method, ctx);
        }
        if (!createEventEmitted)
            _eventEmitter.EmitCreateInitializers(declaration, ctx);
        foreach (var member in declaration.Members.Where(m => m is not FieldDeclarationNode and not ConstructorDeclarationNode and not MethodDeclarationNode))
            ctx.Dispatch(member, ctx);
        if (ctx.CurrentType is not null)
            staticCtorEmitter.EmitStaticCtor(ctx.CurrentType, declaration.Members, ctx);
    }

    private static void EmitDefaultConstructor(EmitContext ctx)
        => ConstructorEmitter.EmitImplicit(ctx.CurrentType!, ctx);

    private static TypeSymbol? ResolveType(EmitContext ctx, string name, int arity) =>
        ctx.Symbols.TryResolve(name, arity, ctx.CurrentNamespacePrefix, [], out var symbol) ? symbol : null;

}
