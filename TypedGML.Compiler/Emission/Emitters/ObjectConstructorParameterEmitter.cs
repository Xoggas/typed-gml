using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Declarations;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Emission.Emitters;

internal sealed class ObjectConstructorParameterEmitter
{
    public bool HasAssignments(
        IReadOnlyList<ParameterNode> parameters,
        IReadOnlySet<string> assignedMembers,
        EmitContext ctx) =>
        parameters.Any(parameter => Assignment(parameter, assignedMembers, ctx) is not null);

    public void Emit(
        IReadOnlyList<ParameterNode> parameters,
        IReadOnlySet<string> assignedMembers,
        EmitContext ctx)
    {
        var assignments = parameters
            .Select(parameter => Assignment(parameter, assignedMembers, ctx))
            .Where(assignment => assignment is not null)
            .ToList();
        foreach (var assignment in assignments)
            ctx.Writer.WriteLine(assignment!);
    }

    private static string? Assignment(ParameterNode parameter, IReadOnlySet<string> assignedMembers, EmitContext ctx)
    {
        if (!TryFindTarget(parameter.Name, ctx, out var owner, out var member))
            return null;
        if (assignedMembers.Contains(member.Name))
            return null;

        return member.Kind == MemberKind.Field
            ? $"{member.Name} = {parameter.Name};"
            : PropertyAssignment(owner, member, parameter.Name, ctx);
    }

    private static string PropertyAssignment(TypeSymbol owner, MemberSymbol member, string value, EmitContext ctx)
    {
        if (member.NativePropertyName is not null)
            return $"{member.NativePropertyName} = {value};";
        if (IsAutoProperty(owner, member.Name, ctx))
            return $"{NamingConvention.InstancePropertyBackingName("self", member.Name)} = {value};";
        return $"{NamingConvention.PropertySetter(owner, member)}(self, {value});";
    }

    private static bool TryFindTarget(
        string parameterName,
        EmitContext ctx,
        out TypeSymbol owner,
        out MemberSymbol member)
    {
        for (var current = ctx.CurrentType; current is not null; current = current.Base)
        {
            member = current.Members.FirstOrDefault(candidate =>
                candidate.Kind is MemberKind.Field or MemberKind.Property &&
                !candidate.Modifiers.Contains("static", StringComparer.Ordinal) &&
                !string.Equals(candidate.Name, parameterName, StringComparison.Ordinal) &&
                string.Equals(candidate.Name, parameterName, StringComparison.OrdinalIgnoreCase))!;
            if (member is not null)
            {
                owner = current;
                return true;
            }
        }

        owner = null!;
        member = null!;
        return false;
    }

    private static bool IsAutoProperty(TypeSymbol owner, string propertyName, EmitContext ctx)
    {
        if (!ctx.TypeDeclarations.TryGetValue(TypeDeclarationMapBuilder.Key(owner), out var node))
            return false;

        var members = node switch
        {
            ClassDeclarationNode @class => @class.Members,
            StructDeclarationNode @struct => @struct.Members,
            _ => []
        };
        return members.OfType<PropertyDeclarationNode>().Any(property =>
            property.Name == propertyName &&
            property.Accessors.Count > 0 &&
            property.Accessors.All(accessor => accessor.Body is null) &&
            DecoratorArg(property.Decorators, "NativeProperty") is null &&
            DecoratorArg(property.Decorators, "Asset") is null);
    }

    private static string? DecoratorArg(IReadOnlyList<DecoratorNode> decorators, string name) =>
        decorators.FirstOrDefault(d => d.Name == name)?.Args.FirstOrDefault() is LiteralExpressionNode literal
            ? literal.Value?.ToString()
            : null;
}
