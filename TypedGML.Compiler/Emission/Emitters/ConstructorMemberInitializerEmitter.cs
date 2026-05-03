using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Emission.Emitters;

internal static class ConstructorMemberInitializerEmitter
{
    public static void EmitDefaults(TypeSymbol? type, IAstNode? body, EmitContext ctx)
    {
        var assignedMembers = body is null
            ? new HashSet<string>(StringComparer.Ordinal)
            : ConstructorFieldAssignmentFinder.Find(body);
        EmitDefaults(type, assignedMembers, ctx);
    }

    public static void EmitDefaults(TypeSymbol? type, IReadOnlySet<string> assignedMembers, EmitContext ctx)
    {
        ConstructorFieldInitializerEmitter.Emit(type, assignedMembers, ctx);
        ConstructorAutoPropertyInitializerEmitter.Emit(type, assignedMembers, ctx);
        ConstructorEventInitializerEmitter.Emit(type, ctx);
    }

    public static void EmitChainDefaults(
        IReadOnlyList<ConstructorInlineFrame> chain,
        IAstNode? currentBody,
        EmitContext ctx)
    {
        var assignedMembers = ConstructorFieldAssignmentFinder.Find(currentBody);
        foreach (var frame in chain)
            if (frame.Body is not null)
                assignedMembers.UnionWith(ConstructorFieldAssignmentFinder.Find(frame.Body));

        foreach (var frame in chain)
            WithCurrentType(ctx, frame.Type, () => EmitDefaults(frame.Type, assignedMembers, ctx));
    }

    private static void WithCurrentType(EmitContext ctx, TypeSymbol type, Action action)
    {
        var previousType = ctx.CurrentType;
        ctx.CurrentType = type;
        try
        {
            action();
        }
        finally
        {
            ctx.CurrentType = previousType;
        }
    }
}
