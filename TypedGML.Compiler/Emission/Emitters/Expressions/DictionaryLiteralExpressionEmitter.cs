using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;

namespace TypedGML.Compiler.Emission.Emitters.Expressions;

public sealed class DictionaryLiteralExpressionEmitter : INodeEmitter
{
    public bool Matches(IAstNode node) => node is DictionaryLiteralExpressionNode;

    public void Emit(IAstNode node, EmitContext ctx)
    {
        var expression = (DictionaryLiteralExpressionNode)node;
        var entries = string.Join(" ", expression.Entries.Select(entry =>
            $"ds_map_add(__map, {ctx.Emitter.Render(entry.Key, ctx)}, {ctx.Emitter.Render(entry.Value, ctx)});"));
        ctx.Writer.Write($"(function() {{ var __map = ds_map_create(); {entries} return __map; }})()");
    }
}
