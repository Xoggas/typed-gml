using TypedGML.Compiler.Ast.Declarations;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Ast.Statements;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Emission.Emitters;

internal sealed class ClassEventEmitter
{
    private readonly DecoratorProcessor _decoratorProcessor = new();
    private readonly ClassCreateInitializerEmitter _createInitializerEmitter = new();

    public void Emit(ClassDeclarationNode declaration, MethodDeclarationNode method, string eventName, EmitContext ctx)
    {
        if (method.Body is not BlockStatementNode block || ctx.CurrentType is null)
            return;

        var resolvedEvent = GmlEventMap.Resolve(eventName);
        EmitEventFile(declaration, resolvedEvent, ctx, eventCtx =>
        {
            foreach (var statement in block.Statements)
                eventCtx.Dispatch(statement, eventCtx);
        });
    }

    public void EmitCollision(ClassDeclarationNode declaration, MethodDeclarationNode method, string collisionTargetFileName, EmitContext ctx)
    {
        if (method.Body is not BlockStatementNode block || ctx.CurrentType is null)
            return;

        EmitEventFile(declaration, collisionTargetFileName, ctx, eventCtx =>
        {
            foreach (var statement in block.Statements)
                eventCtx.Dispatch(statement, eventCtx);
        }, true);
    }

    public void EmitCreateInitializers(ClassDeclarationNode declaration, EmitContext ctx)
    {
        if (ctx.CurrentType is null || !_createInitializerEmitter.HasInitializers(declaration, ctx))
            return;

        EmitEventFile(declaration, GmlEventMap.Resolve("Create"), ctx, _ => { });
    }

    public string? ResolveEventName(MethodDeclarationNode method, TypeSymbol? type)
    {
        var own = DecoratorArg(method.Decorators, "NativeEvent");
        if (own is not null)
            return own;

        for (var current = type?.Base; current is not null; current = current.Base)
        {
            var member = current.Members.FirstOrDefault(m =>
                m.Kind == MemberKind.Method &&
                m.Name == method.Name &&
                m.Parameters.Select(p => p.TypeRef).SequenceEqual(method.Parameters.Select(p => p.TypeRef), StringComparer.Ordinal));
            if (!string.IsNullOrEmpty(member?.NativeEventName))
                return member.NativeEventName;
        }

        return null;
    }

    public string? ResolveCollisionTargetFileName(MethodDeclarationNode method, EmitContext ctx) =>
        method.Decorators.Any(d => d.Name == "Collision")
            ? _decoratorProcessor.Process(
                method.Decorators,
                ctx.Diagnostics,
                ctx.Symbols,
                ctx.CurrentType,
                ctx.CurrentNamespacePrefix,
                ctx.UsingPrefixes).CollisionTargetFileName
            : null;

    private void EmitEventFile(
        ClassDeclarationNode declaration,
        string resolvedEvent,
        EmitContext ctx,
        Action<EmitContext> emitBody,
        bool isCollision = false)
    {
        var type = ctx.CurrentType;
        if (type is null)
            return;

        var eventWriter = new GmlWriter();
        var eventCtx = ctx.WithWriter(eventWriter);
        eventCtx.IsObjectEventContext = true;
        eventCtx.IsInObjectMethod = true;
        eventCtx.SelfName = "self";
        eventCtx.ResetTempVars();
        eventCtx.Scope.Push();
        if (string.Equals(resolvedEvent, "Create_0", StringComparison.Ordinal))
            _createInitializerEmitter.Emit(declaration, eventCtx);
        emitBody(eventCtx);
        eventCtx.Scope.Pop();
        var path = isCollision
            ? ctx.Files.GetCollisionEventPath(type, resolvedEvent)
            : ctx.Files.GetEventPath(type, resolvedEvent);
        ctx.Output.Write(path, eventWriter.GetOutput());
    }

    private static string? DecoratorArg(IReadOnlyList<DecoratorNode> decorators, string name) =>
        decorators.FirstOrDefault(d => d.Name == name)?.Args.FirstOrDefault() is LiteralExpressionNode literal
            ? literal.Value?.ToString()
            : null;
}
