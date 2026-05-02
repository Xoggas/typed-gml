using TypedGML.Compiler.Ast.Declarations;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Ast.Statements;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Emission.Emitters;

internal sealed class ClassEventEmitter
{
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

    public void EmitCreateInitializers(ClassDeclarationNode declaration, EmitContext ctx)
    {
        if (ctx.CurrentType is null || !declaration.Members.OfType<FieldDeclarationNode>().Any(HasInstanceInitializer))
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

    private static void EmitEventFile(
        ClassDeclarationNode declaration,
        string resolvedEvent,
        EmitContext ctx,
        Action<EmitContext> emitBody)
    {
        var type = ctx.CurrentType;
        if (type is null)
            return;

        var eventWriter = new GmlWriter();
        var eventCtx = ctx.WithWriter(eventWriter);
        eventCtx.IsObjectEventContext = true;
        eventCtx.SelfName = null;
        eventCtx.ResetTempVars();
        eventCtx.Scope.Push();
        EmitCreateInitializers(declaration, resolvedEvent, eventWriter, eventCtx);
        emitBody(eventCtx);
        eventCtx.Scope.Pop();
        var path = ctx.Files.GetEventPath(type, resolvedEvent);
        ctx.Output.Write(path, eventWriter.GetOutput());
    }

    private static void EmitCreateInitializers(
        ClassDeclarationNode declaration,
        string resolvedEvent,
        GmlWriter eventWriter,
        EmitContext eventCtx)
    {
        if (!string.Equals(resolvedEvent, "Create_0", StringComparison.Ordinal))
            return;

        foreach (var field in declaration.Members.OfType<FieldDeclarationNode>().Where(HasInstanceInitializer))
        {
            var value = eventCtx.RenderWithTempPrelude(field.Initializer);
            eventCtx.FlushTempPrelude();
            eventWriter.WriteLine($"{field.Name} = {value};");
        }
    }

    private static bool HasInstanceInitializer(FieldDeclarationNode field) =>
        field.Initializer is not null &&
        !field.Modifiers.Contains("static", StringComparer.Ordinal) &&
        !field.Modifiers.Contains("const", StringComparer.Ordinal);

    private static string? DecoratorArg(IReadOnlyList<DecoratorNode> decorators, string name) =>
        decorators.FirstOrDefault(d => d.Name == name)?.Args.FirstOrDefault() is LiteralExpressionNode literal
            ? literal.Value?.ToString()
            : null;
}
