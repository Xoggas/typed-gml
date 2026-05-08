using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Declarations;
using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Emission.Emitters;

internal static class ConstructorAutoPropertyInitializerEmitter
{
    public static void Emit(TypeSymbol? type, EmitContext ctx) =>
        Emit(type, new HashSet<string>(StringComparer.Ordinal), ctx);

    public static void Emit(TypeSymbol? type, IReadOnlySet<string> assignedProperties, EmitContext ctx)
    {
        if (type is null)
            return;

        foreach (var property in AutoProperties(type, ctx))
        {
            if (property.Initializer is null && assignedProperties.Contains(property.Name))
                continue;

            var target = NamingConvention.InstancePropertyBackingName(ctx.SelfName ?? EmitContext.InstParam, property.Name);
            var value = AutoPropertyEmitterHelper.RenderInitialValue(property, ctx);
            ctx.FlushTempPrelude();
            ctx.Writer.WriteLine($"{target} = {value};");
        }
    }

    private static IEnumerable<PropertyDeclarationNode> AutoProperties(TypeSymbol type, EmitContext ctx)
    {
        if (!ctx.TypeDeclarations.TryGetValue(TypeDeclarationMapBuilder.Key(type), out var declaration))
            return [];

        return declaration switch
        {
            ClassDeclarationNode @class => InstanceAutoProperties(@class.Members),
            StructDeclarationNode @struct => InstanceAutoProperties(@struct.Members),
            _ => []
        };
    }

    private static IEnumerable<PropertyDeclarationNode> InstanceAutoProperties(IEnumerable<IAstNode> members) =>
        members.OfType<PropertyDeclarationNode>().Where(IsInstanceAutoProperty);

    private static bool IsInstanceAutoProperty(PropertyDeclarationNode property) =>
        !property.Modifiers.Contains("static", StringComparer.Ordinal) &&
        !property.Modifiers.Contains("abstract", StringComparer.Ordinal) &&
        AutoPropertyEmitterHelper.IsCompilerBacked(property);
}
