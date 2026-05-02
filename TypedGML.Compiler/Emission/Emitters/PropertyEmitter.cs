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
        if (ctx.CurrentType is null ||
            property.Modifiers.Contains("static", StringComparer.Ordinal) ||
            property.Modifiers.Contains("abstract", StringComparer.Ordinal))
            return;

        var symbol = ctx.CurrentType.Members.FirstOrDefault(m => m.Kind == MemberKind.Property && m.Name == property.Name);
        if (symbol is null)
            return;

        foreach (var accessor in property.Accessors.Where(accessor => accessor.Kind == AccessorKind.Get))
            EmitAccessor(ctx, property, symbol, accessor);
        foreach (var accessor in property.Accessors.Where(accessor => accessor.Kind == AccessorKind.Set))
            EmitAccessor(ctx, property, symbol, accessor);
    }

    private static void EmitAccessor(EmitContext ctx, PropertyDeclarationNode property, MemberSymbol symbol, AccessorNode accessor)
    {
        var target = PropertyTarget(property);
        var selfName = ctx.CurrentType?.ObjectAssetName is null ? "self" : null;
        if (accessor.Kind == AccessorKind.Get)
        {
            ctx.Writer.Write($"function {NamingConvention.PropertyGetter(ctx.CurrentType!, symbol)}({ParameterList(selfName)})");
            ctx.ResetTempVars();
            if (accessor.Body is null || target is not null)
            {
                ctx.Writer.BeginBlock();
                ctx.Writer.WriteLine($"return {AccessorTarget(target, property, selfName)};");
                ctx.Writer.EndBlock();
            }
            else
                WithAccessorContext(ctx, symbol, selfName, false, () => ctx.Dispatch(accessor.Body, ctx));
            return;
        }

        ctx.Writer.Write($"function {NamingConvention.PropertySetter(ctx.CurrentType!, symbol)}({ParameterList(selfName, "value")})");
        ctx.ResetTempVars();
        if (accessor.Body is null || target is not null)
        {
            ctx.Writer.BeginBlock();
            ctx.Writer.WriteLine($"{AccessorTarget(target, property, selfName)} = value;");
            ctx.Writer.EndBlock();
        }
        else
            WithAccessorContext(ctx, symbol, selfName, true, () => ctx.Dispatch(accessor.Body, ctx));
    }

    private static string? PropertyTarget(PropertyDeclarationNode property)
    {
        var native = DecoratorArg(property.Decorators, "NativeProperty");
        if (native is not null)
            return native;
        return DecoratorArg(property.Decorators, "Asset");
    }

    private static string? DecoratorArg(IReadOnlyList<DecoratorNode> decorators, string name) =>
        decorators.FirstOrDefault(d => d.Name == name)?.Args.FirstOrDefault() is LiteralExpressionNode literal
            ? literal.Value?.ToString()
            : null;

    private static string ParameterList(string? selfName, string? extra = null) =>
        string.Join(", ", string.IsNullOrEmpty(selfName)
            ? (extra is null ? [] : [extra])
            : (extra is null ? [selfName] : [selfName, extra]));

    private static string AccessorTarget(string? target, PropertyDeclarationNode property, string? selfName)
    {
        if (target is not null)
            return string.IsNullOrEmpty(selfName) ? target : $"{selfName}.{target}";
        return string.IsNullOrEmpty(selfName)
            ? NamingConvention.PropertyBackingName(property.Name)
            : NamingConvention.InstancePropertyBackingName(selfName, property.Name);
    }

    private static void WithAccessorContext(EmitContext ctx, MemberSymbol symbol, string? selfName, bool hasValue, Action action)
    {
        var previousMember = ctx.CurrentMember;
        var previousSelf = ctx.SelfName;
        ctx.CurrentMember = symbol;
        ctx.SelfName = selfName;
        ctx.Scope.Push();
        if (hasValue)
            ctx.Scope.Declare("value", symbol.ReturnType);
        try
        {
            action();
        }
        finally
        {
            ctx.Scope.Pop();
            ctx.SelfName = previousSelf;
            ctx.CurrentMember = previousMember;
        }
    }
}
