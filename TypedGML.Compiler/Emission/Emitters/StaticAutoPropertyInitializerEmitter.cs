using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Emission.Emitters;

internal sealed class StaticAutoPropertyInitializerEmitter
{
    public void Emit(TypeSymbol type, IEnumerable<PropertyDeclarationNode> properties, EmitContext ctx)
    {
        foreach (var property in properties.Where(IsAutoProperty))
        {
            var symbol = type.Members.FirstOrDefault(m => m.Kind == MemberKind.Property && m.Name == property.Name);
            if (symbol is null)
                continue;

            var value = AutoPropertyEmitterHelper.RenderInitialValue(property, ctx);
            ctx.FlushTempPrelude();
            ctx.Writer.WriteLine($"{NamingConvention.StaticPropertyBackingName(type, symbol)} = {value};");
        }
    }

    private static bool IsAutoProperty(PropertyDeclarationNode property) =>
        AutoPropertyEmitterHelper.IsCompilerBacked(property);
}
