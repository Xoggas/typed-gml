using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Emission.Emitters;

public sealed class PropertyEmitter : INodeEmitter
{
    public bool Matches(IAstNode node) => node is PropertyDeclarationNode;

    public void Emit(IAstNode node, EmitContext ctx)
    {
        var property = (PropertyDeclarationNode)node;
        if (ctx.CurrentType is null)
            return;

        var symbol = ctx.CurrentType.Members.FirstOrDefault(m => m.Kind == MemberKind.Property && m.Name == property.Name);
        if (symbol is null)
            return;

        foreach (var accessor in property.Accessors)
            EmitAccessor(ctx, property, symbol, accessor);
    }

    private static void EmitAccessor(EmitContext ctx, PropertyDeclarationNode property, MemberSymbol symbol, AccessorNode accessor)
    {
        var target = PropertyTarget(property);
        if (accessor.Kind == AccessorKind.Get)
        {
            ctx.Writer.Write($"function {NamingConvention.PropertyGetter(ctx.CurrentType!, symbol)}()");
            ctx.Writer.BeginBlock();
            if (accessor.Body is null || target is not null)
                ctx.Writer.WriteLine($"return {target ?? $"__backing_{property.Name}"};");
            else
                ctx.Dispatch(accessor.Body, ctx);
            ctx.Writer.EndBlock();
            return;
        }

        ctx.Writer.Write($"function {NamingConvention.PropertySetter(ctx.CurrentType!, symbol)}(value)");
        ctx.Writer.BeginBlock();
        if (accessor.Body is null || target is not null)
            ctx.Writer.WriteLine($"{target ?? $"__backing_{property.Name}"} = value;");
        else
            ctx.Dispatch(accessor.Body, ctx);
        ctx.Writer.EndBlock();
    }

    private static string? PropertyTarget(PropertyDeclarationNode property)
    {
        var native = DecoratorArg(property.Decorators, "NativeProperty");
        if (native is not null)
            return native;
        if (property.Modifiers.Contains("global", StringComparer.Ordinal))
            return $"global.{property.Name}";
        return DecoratorArg(property.Decorators, "Asset");
    }

    private static string? DecoratorArg(IReadOnlyList<DecoratorNode> decorators, string name) =>
        decorators.FirstOrDefault(d => d.Name == name)?.Args.FirstOrDefault() is LiteralExpressionNode literal
            ? literal.Value?.ToString()
            : null;
}
