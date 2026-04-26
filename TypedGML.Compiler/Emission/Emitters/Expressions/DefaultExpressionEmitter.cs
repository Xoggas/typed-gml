using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Emission.Emitters.Expressions;

public sealed class DefaultExpressionEmitter : INodeEmitter
{
    public bool Matches(IAstNode node) => node is DefaultExpressionNode;

    public void Emit(IAstNode node, EmitContext ctx)
    {
        var expression = (DefaultExpressionNode)node;
        if (ctx.CurrentType?.GenericParameters.Any(p => p.Name == expression.TypeName) == true)
        {
            ctx.Writer.Write($"__tgml_default(__genericArgs.{expression.TypeName})");
            return;
        }

        if (expression.TypeName.EndsWith("?", StringComparison.Ordinal) || expression.TypeName.EndsWith("[]", StringComparison.Ordinal))
        {
            ctx.Writer.Write("undefined");
            return;
        }

        if (!ExpressionSymbolHelper.TryResolveType(ctx, expression.TypeName, out var type))
        {
            ctx.Writer.Write(Fallback(expression.TypeName));
            return;
        }

        ctx.Writer.Write(type.Kind switch
        {
            TypeKind.Struct => $"{NamingConvention.ConstructorName(type)}()",
            TypeKind.Enum => "0",
            _ => Fallback(type.QualifiedName)
        });
    }

    private static string Fallback(string typeName) => typeName switch
    {
        "number" => "0",
        "bool" => "false",
        _ => "undefined"
    };
}
