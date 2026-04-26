using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Ast.Members;

namespace TypedGML.Compiler.Emission.Emitters;

public sealed class OperatorEmitter : INodeEmitter
{
    public bool Matches(IAstNode node) => node is OperatorDeclarationNode;

    public void Emit(IAstNode node, EmitContext ctx)
    {
        var operatorNode = (OperatorDeclarationNode)node;
        if (ctx.CurrentType is null)
            return;

        var parameters = string.Join(", ", operatorNode.Parameters.Select(p => p.Name));
        ctx.Writer.Write($"function {NamingConvention.OperatorName(ctx.CurrentType, operatorNode.OperatorSymbol)}({parameters})");
        ctx.Writer.BeginBlock();
        var nativeCall = DecoratorArg(operatorNode.Decorators, "NativeCall");
        if (nativeCall is not null)
            EmitNativeCall(operatorNode, nativeCall, ctx);
        else if (operatorNode.Body is not null)
            ctx.Dispatch(operatorNode.Body, ctx);
        ctx.Writer.EndBlock();
    }

    private static void EmitNativeCall(OperatorDeclarationNode operatorNode, string nativeCall, EmitContext ctx)
    {
        var argNames = operatorNode.Parameters.Select(p => p.Name).ToArray();
        if (IntrinsicOpEmitter.TryEmit(nativeCall, argNames, operatorNode.ReturnType, ctx.Writer))
            return;

        var invocation = $"{nativeCall}({string.Join(", ", argNames)})";
        ctx.Writer.WriteLine($"return {invocation};");
    }

    private static string? DecoratorArg(IReadOnlyList<DecoratorNode> decorators, string name) =>
        decorators.FirstOrDefault(d => d.Name == name)?.Args.FirstOrDefault() is LiteralExpressionNode literal
            ? literal.Value?.ToString()
            : null;
}
