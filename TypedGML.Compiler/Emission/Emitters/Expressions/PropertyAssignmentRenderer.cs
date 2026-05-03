using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Declarations;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Emission.Emitters.Expressions;

internal static class PropertyAssignmentRenderer
{
    public static string Render(
        AssignmentExpressionNode expression,
        TypeSymbol owner,
        MemberSymbol member,
        string value,
        EmitContext ctx)
    {
        if (TryRenderConstructorBackingAssignment(expression, owner, member, value, ctx, out var rendered))
            return rendered;

        if (expression.Target is IdentifierExpressionNode)
            return WriteCurrentProperty(expression.Op, owner, member, value, ctx);

        var access = (MemberAccessExpressionNode)expression.Target;
        var target = ctx.Emitter.Render(access.Target, ctx);
        if (member.NativePropertyName is not null)
            return $"{target}.{member.NativePropertyName} {expression.Op} {value}";
        if (expression.Op == "=")
            return $"{NamingConvention.PropertySetter(owner, member)}({target}, {value})";
        var op = expression.Op[..^1];
        return $"{NamingConvention.PropertySetter(owner, member)}({target}, {NamingConvention.PropertyGetter(owner, member)}({target}) {op} {value})";
    }

    private static bool TryRenderConstructorBackingAssignment(
        AssignmentExpressionNode expression,
        TypeSymbol owner,
        MemberSymbol member,
        string value,
        EmitContext ctx,
        out string rendered)
    {
        rendered = string.Empty;
        if (!ctx.IsInConstructor || expression.Op != "=" || !IsCompilerBacked(member))
            return false;
        if (!TryGetProperty(owner, member.Name, ctx, out var property) || !IsReadOnlyAutoProperty(property))
            return false;

        rendered = $"{BackingTarget(expression.Target, property.Name, ctx)} = {value}";
        return true;
    }

    private static string WriteCurrentProperty(string op, TypeSymbol owner, MemberSymbol member, string value, EmitContext ctx)
    {
        if (ctx.IsObjectEventContext && member.NativePropertyName is not null)
            return $"{member.NativePropertyName} {op} {value}";
        if (member.NativePropertyName is not null && ctx.SelfName is not null)
            return $"{ctx.SelfName}.{member.NativePropertyName} {op} {value}";
        var selfName = ctx.SelfName ?? "self";
        if (op == "=")
            return $"{NamingConvention.PropertySetter(owner, member)}({selfName}, {value})";
        var read = $"{NamingConvention.PropertyGetter(owner, member)}({selfName})";
        return $"{NamingConvention.PropertySetter(owner, member)}({selfName}, {read} {op[..^1]} {value})";
    }

    private static string BackingTarget(IAstNode target, string propertyName, EmitContext ctx)
    {
        if (target is IdentifierExpressionNode)
            return string.IsNullOrEmpty(ctx.SelfName)
                ? NamingConvention.PropertyBackingName(propertyName)
                : NamingConvention.InstancePropertyBackingName(ctx.SelfName, propertyName);

        var access = (MemberAccessExpressionNode)target;
        return $"{ctx.Emitter.Render(access.Target, ctx)}.{NamingConvention.PropertyBackingName(propertyName)}";
    }

    private static bool TryGetProperty(TypeSymbol owner, string name, EmitContext ctx, out PropertyDeclarationNode property)
    {
        property = null!;
        if (!ctx.TypeDeclarations.TryGetValue(TypeDeclarationMapBuilder.Key(owner), out var declaration))
            return false;

        var members = declaration switch
        {
            ClassDeclarationNode @class => @class.Members,
            StructDeclarationNode @struct => @struct.Members,
            _ => []
        };
        property = members.OfType<PropertyDeclarationNode>().FirstOrDefault(p => p.Name == name)!;
        return property is not null;
    }

    private static bool IsReadOnlyAutoProperty(PropertyDeclarationNode property) =>
        !property.Modifiers.Contains("static", StringComparer.Ordinal) &&
        property.Accessors.Count > 0 &&
        property.Accessors.All(accessor => accessor.Body is null) &&
        property.Accessors.Any(accessor => accessor.Kind == AccessorKind.Get) &&
        property.Accessors.All(accessor => accessor.Kind != AccessorKind.Set);

    private static bool IsCompilerBacked(MemberSymbol member) =>
        member.NativePropertyName is null && member.AssetName is null;
}
