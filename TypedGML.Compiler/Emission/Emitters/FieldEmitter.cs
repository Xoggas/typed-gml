using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Emission.Emitters;

public sealed class FieldEmitter : INodeEmitter
{
    public bool Matches(IAstNode node) => node is FieldDeclarationNode;

    public void Emit(IAstNode node, EmitContext ctx)
    {
        var field = (FieldDeclarationNode)node;
        if (ctx.CurrentType is null || !field.Modifiers.Contains("const", StringComparer.Ordinal))
            return;

        var symbol = ctx.CurrentType.Members.FirstOrDefault(m => m.Kind == MemberKind.Field && m.Name == field.Name);
        if (symbol is null)
            return;

        ctx.Writer.WriteLine($"#macro {NamingConvention.ConstMacro(ctx.CurrentType, symbol)} {FormatValue(field.Initializer)}");
    }

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
        MemberAccessExpressionNode access => $"{FormatValue(access.Target)}_{access.MemberName}",
        _ => "undefined"
    };
}
