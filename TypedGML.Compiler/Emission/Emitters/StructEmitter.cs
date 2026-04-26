using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Declarations;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Emission.Emitters;

public sealed class StructEmitter(StaticCtorEmitter staticCtorEmitter) : INodeEmitter
{
    public bool Matches(IAstNode node) => node is StructDeclarationNode;

    public void Emit(IAstNode node, EmitContext ctx)
    {
        var declaration = (StructDeclarationNode)node;
        var previousType = ctx.CurrentType;
        ctx.CurrentType = ResolveType(ctx, declaration.Name);
        EmitCreate(declaration, ctx);
        foreach (var member in declaration.Members.Where(m => m is not ConstructorDeclarationNode))
            ctx.Dispatch(member, ctx);
        if (ctx.CurrentType is not null)
            staticCtorEmitter.EmitStaticCtor(ctx.CurrentType, declaration.Members, ctx);
        ctx.CurrentType = previousType;
    }

    private static void EmitCreate(StructDeclarationNode declaration, EmitContext ctx)
    {
        ctx.Writer.Write($"function {NamingConvention.ConstructorName(ctx.CurrentType!)}()");
        ctx.Writer.BeginBlock();
        var fields = declaration.Members
            .OfType<FieldDeclarationNode>()
            .Where(f => !f.Modifiers.Contains("const", StringComparer.Ordinal) && !f.Modifiers.Contains("static", StringComparer.Ordinal))
            .ToList();
        if (fields.Count == 0)
        {
            ctx.Writer.WriteLine("return {};");
            ctx.Writer.EndBlock();
            return;
        }

        ctx.Writer.WriteLine("return {");
        ctx.Writer.Indent();
        for (var i = 0; i < fields.Count; i++)
        {
            var suffix = i < fields.Count - 1 ? "," : string.Empty;
            ctx.Writer.WriteLine($"{fields[i].Name}: {FormatValue(fields[i].Initializer)}{suffix}");
        }
        ctx.Writer.Dedent();
        ctx.Writer.WriteLine("};");
        ctx.Writer.EndBlock();
    }

    private static TypeSymbol? ResolveType(EmitContext ctx, string name) =>
        ctx.Symbols.TryResolve(name, ctx.CurrentNamespacePrefix, [], out var symbol) ? symbol : null;

    private static string FormatValue(IAstNode? node) => node switch
    {
        LiteralExpressionNode literal => literal.Kind switch
        {
            LiteralKind.String => $"\"{literal.Value?.ToString()?.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal)}\"",
            LiteralKind.Bool => string.Equals(literal.Value?.ToString(), "true", StringComparison.OrdinalIgnoreCase) ? "true" : "false",
            LiteralKind.Null => "undefined",
            _ => literal.Value?.ToString() ?? "undefined"
        },
        IdentifierExpressionNode identifier => identifier.Name,
        _ => "undefined"
    };
}
