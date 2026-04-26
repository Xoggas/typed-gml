using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;

namespace TypedGML.Compiler.Emission.Emitters.Expressions;

public sealed class DictionaryLiteralExpressionEmitter : INodeEmitter
{
    public bool Matches(IAstNode node) => node is DictionaryLiteralExpressionNode;

    public void Emit(IAstNode node, EmitContext ctx)
    {
        var expression = (DictionaryLiteralExpressionNode)node;
        var ctorName = ResolveDictionaryCtorName(ctx);

        if (expression.Entries.Count == 0)
        {
            ctx.Writer.Write($"{ctorName}()");
            return;
        }

        var adds = string.Join(" ", expression.Entries.Select(entry =>
            $"__d.Add({ctx.Emitter.Render(entry.Key, ctx)}, {ctx.Emitter.Render(entry.Value, ctx)});"));
        ctx.Writer.Write($"(function() {{ var __d = {ctorName}(); {adds} return __d; }})()");
    }

    private static string ResolveDictionaryCtorName(EmitContext ctx) =>
        ctx.Symbols.TryResolve("Dictionary", ctx.CurrentNamespacePrefix, ["TypedGML.Collections"], out var dictType)
            ? NamingConvention.ConstructorName(dictType)
            : "Dictionary_create";
}
