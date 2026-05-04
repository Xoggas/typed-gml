using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Declarations;
using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Emission.Emitters;

internal static class ConstructorEventInitializerEmitter
{
    public static void Emit(TypeSymbol? type, EmitContext ctx)
    {
        if (type is null)
            return;

        foreach (var evt in Events(type, ctx))
        {
            var symbol = type.Members.FirstOrDefault(member => member.Kind == MemberKind.Event && member.Name == evt.Name);
            if (symbol is not null)
                ctx.Writer.WriteLine($"{NamingConvention.InstanceEventBackingName(ctx.SelfName ?? EmitContext.InstParam, symbol)} = [];");
        }
    }

    private static IEnumerable<EventDeclarationNode> Events(TypeSymbol type, EmitContext ctx)
    {
        if (!ctx.TypeDeclarations.TryGetValue(TypeDeclarationMapBuilder.Key(type), out var declaration))
            return [];

        return declaration switch
        {
            ClassDeclarationNode @class => InstanceEvents(@class.Members),
            StructDeclarationNode @struct => InstanceEvents(@struct.Members),
            _ => []
        };
    }

    private static IEnumerable<EventDeclarationNode> InstanceEvents(IEnumerable<IAstNode> members) =>
        members.OfType<EventDeclarationNode>().Where(evt => !evt.Modifiers.Contains("static", StringComparer.Ordinal));
}
