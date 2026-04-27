using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Ast.Statements;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Emission.Emitters;

public sealed class MethodEmitter : INodeEmitter
{
    public bool Matches(IAstNode node) => node is MethodDeclarationNode;

    public void Emit(IAstNode node, EmitContext ctx)
    {
        var method = (MethodDeclarationNode)node;
        if (ctx.CurrentType is null ||
            method.Modifiers.Contains("static", StringComparer.Ordinal) ||
            method.Modifiers.Contains("abstract", StringComparer.Ordinal))
            return;

        var symbol = ResolveSymbol(ctx.CurrentType, method);
        var functionName = symbol is null ? $"{NamingConvention.TypeName(ctx.CurrentType)}_{method.Name}" : NamingConvention.MethodName(ctx.CurrentType, symbol);
        var selfName = ctx.CurrentType.ObjectAssetName is null ? "self" : null;
        var parameters = string.Join(", ", ParameterNames(method, selfName));
        var nativeCall = DecoratorArg(method.Decorators, "NativeCall");
        WithMemberContext(ctx, symbol, selfName, method.Parameters, () =>
        {
            ctx.Writer.Write($"function {functionName}({parameters})");
            if (nativeCall is not null)
            {
                ctx.Writer.BeginBlock();
                EmitNativeCall(ctx, method, nativeCall);
                ctx.Writer.EndBlock();
            }
            else if (InlineBody(method, ctx) is { } inlineBody)
                ctx.Dispatch(inlineBody, ctx);
            else if (method.Body is not null)
                ctx.Dispatch(method.Body, ctx);
            else
            {
                ctx.Writer.BeginBlock();
                ctx.Writer.EndBlock();
            }
        });
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

    private static IEnumerable<string> ParameterNames(MethodDeclarationNode method, string? selfName) =>
        string.IsNullOrEmpty(selfName) ? method.Parameters.Select(p => p.Name) : [selfName, .. method.Parameters.Select(p => p.Name)];

    private static void WithMemberContext(
        EmitContext ctx,
        MemberSymbol? symbol,
        string? selfName,
        IReadOnlyList<ParameterNode> parameters,
        Action action)
    {
        var previousMember = ctx.CurrentMember;
        var previousSelf = ctx.SelfName;
        ctx.CurrentMember = symbol;
        ctx.SelfName = selfName;
        ctx.Scope.Push();
        foreach (var parameter in parameters)
            ctx.Scope.Declare(parameter.Name, parameter.TypeRef);
        try
        {
            action();
        }
        finally
        {
            ctx.Scope.Pop();
            ctx.SelfName = previousSelf;
            ctx.CurrentMember = previousMember;
        }
    }

    private static IAstNode? InlineBody(MethodDeclarationNode method, EmitContext ctx)
    {
        if (method.Body is not BlockStatementNode
            {
                Statements:
                [
                    ReturnStatementNode
                    {
                        Value: InvocationExpressionNode
                        {
                            Target: IdentifierExpressionNode identifier,
                            PositionalArgs.Count: 0,
                            NamedArgs.Count: 0
                        }
                    }
                ]
            })
            return null;

        if (!ctx.TypeDeclarations.TryGetValue(ctx.CurrentType!.QualifiedName, out var declaration) || declaration is not Ast.Declarations.ClassDeclarationNode @class)
            return null;

        return @class.Members.OfType<MethodDeclarationNode>()
            .FirstOrDefault(candidate =>
                !ReferenceEquals(candidate, method) &&
                candidate.Name == identifier.Name &&
                candidate.Parameters.Count == 0 &&
                candidate.Body is not null)
            ?.Body;
    }

    private static string? DecoratorArg(IReadOnlyList<DecoratorNode> decorators, string name) =>
        decorators.FirstOrDefault(d => d.Name == name)?.Args.FirstOrDefault() is LiteralExpressionNode literal
            ? literal.Value?.ToString()
            : null;
}
