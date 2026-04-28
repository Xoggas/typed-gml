using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Declarations;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Emission.Emitters;

internal static class ConstructorFieldInitializerEmitter
{
    public static void Emit(TypeSymbol? type, IAstNode? body, EmitContext ctx)
    {
        if (type is null)
            return;

        var assignedFields = body is null
            ? new HashSet<string>(StringComparer.Ordinal)
            : ConstructorFieldAssignmentFinder.Find(body);

        foreach (var field in Fields(type, ctx))
        {
            if (field.Initializer is not null)
            {
                ctx.Writer.WriteLine($"self.{field.Name} = {ctx.Emitter.Render(field.Initializer, ctx)};");
                continue;
            }

            if (!assignedFields.Contains(field.Name))
                ctx.Writer.WriteLine($"self.{field.Name} = {DefaultValueRenderer.Render(new DefaultExpressionNode(field.TypeRef, field.Location), ctx)};");
        }
    }

    private static IEnumerable<FieldDeclarationNode> Fields(TypeSymbol type, EmitContext ctx)
    {
        if (!ctx.TypeDeclarations.TryGetValue(type.QualifiedName, out var declaration))
            return [];

        return declaration switch
        {
            ClassDeclarationNode @class => InstanceFields(@class.Members),
            StructDeclarationNode @struct => InstanceFields(@struct.Members),
            _ => []
        };
    }

    private static IEnumerable<FieldDeclarationNode> InstanceFields(IEnumerable<IAstNode> members) =>
        members.OfType<FieldDeclarationNode>().Where(field =>
            !field.Modifiers.Contains("static", StringComparer.Ordinal) &&
            !field.Modifiers.Contains("const", StringComparer.Ordinal));
}
