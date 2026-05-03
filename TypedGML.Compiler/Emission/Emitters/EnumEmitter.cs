using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Declarations;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Emission.Emitters;

public sealed class EnumEmitter : INodeEmitter
{
    public bool Matches(IAstNode node) => node is EnumDeclarationNode;

    public void Emit(IAstNode node, EmitContext ctx)
    {
        var declaration = (EnumDeclarationNode)node;
        var type = ResolveType(ctx, declaration.Name);
        var nextValue = 0;
        foreach (var member in declaration.Members)
        {
            var value = member.Value is null ? nextValue.ToString() : FormatValue(member.Value);
            ctx.Writer.WriteLine($"#macro {NamingConvention.EnumMember(type!, member.Name)} {value}");
            nextValue = member.Value is LiteralExpressionNode literal && int.TryParse(literal.Value?.ToString(), out var parsed)
                ? parsed + 1
                : nextValue + 1;
        }
    }

    private static TypeSymbol? ResolveType(EmitContext ctx, string name) =>
        ctx.Symbols.TryResolve(name, ctx.CurrentNamespacePrefix, [], out var symbol) ? symbol : null;

    private static string FormatValue(IAstNode node) => node switch
    {
        LiteralExpressionNode literal when literal.Kind == LiteralKind.String =>
            $"\"{literal.Value?.ToString()?.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal)}\"",
        LiteralExpressionNode literal when literal.Kind == LiteralKind.Null => "undefined",
        LiteralExpressionNode literal => literal.Value?.ToString() ?? "0",
        IdentifierExpressionNode identifier => identifier.Name,
        _ => "0"
    };
}
