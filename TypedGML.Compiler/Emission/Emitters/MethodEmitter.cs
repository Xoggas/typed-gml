using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Emission.Emitters;

public sealed class MethodEmitter : INodeEmitter
{
    public bool Matches(IAstNode node) => node is MethodDeclarationNode;

    public void Emit(IAstNode node, EmitContext ctx)
    {
        var method = (MethodDeclarationNode)node;
        if (ctx.CurrentType is null || method.Modifiers.Contains("static", StringComparer.Ordinal))
            return;

        var symbol = ResolveSymbol(ctx.CurrentType, method);
        var functionName = symbol is null ? $"{NamingConvention.TypeName(ctx.CurrentType)}_{method.Name}" : NamingConvention.MethodName(ctx.CurrentType, symbol);
        var parameters = string.Join(", ", method.Parameters.Select(p => p.Name));
        ctx.Writer.Write($"function {functionName}({parameters})");
        ctx.Writer.BeginBlock();
        var nativeCall = DecoratorArg(method.Decorators, "NativeCall");
        if (nativeCall is not null)
            EmitNativeCall(ctx, method, nativeCall);
        else if (method.Body is not null)
            ctx.Dispatch(method.Body, ctx);
        ctx.Writer.EndBlock();
    }

    private static void EmitNativeCall(EmitContext ctx, MethodDeclarationNode method, string nativeCall)
    {
        var argNames = method.Parameters.Select(p => p.Name).ToArray();
        if (IntrinsicOpEmitter.TryEmit(nativeCall, argNames, method.TypeRef, ctx.Writer))
            return;

        var invocation = $"{nativeCall}({string.Join(", ", argNames)})";
        if (string.Equals(method.TypeRef, "void", StringComparison.Ordinal))
            ctx.Writer.WriteLine($"{invocation};");
        else
            ctx.Writer.WriteLine($"return {invocation};");
    }

    private static MemberSymbol? ResolveSymbol(TypeSymbol type, MethodDeclarationNode method) =>
        type.Members.FirstOrDefault(member =>
            member.Kind == MemberKind.Method &&
            member.Name == method.Name &&
            member.Parameters.Select(p => p.TypeRef).SequenceEqual(method.Parameters.Select(p => p.TypeRef), StringComparer.Ordinal));

    private static string? DecoratorArg(IReadOnlyList<DecoratorNode> decorators, string name) =>
        decorators.FirstOrDefault(d => d.Name == name)?.Args.FirstOrDefault() is LiteralExpressionNode literal
            ? literal.Value?.ToString()
            : null;
}
