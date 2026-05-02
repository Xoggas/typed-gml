using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Declarations;
using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Emission.Emitters;

public sealed class FunctionEmitter : INodeEmitter
{
    public bool Matches(IAstNode node) => node is FunctionDeclarationNode;

    public void Emit(IAstNode node, EmitContext ctx)
    {
        var function = (FunctionDeclarationNode)node;
        var parameters = string.Join(", ", function.Parameters.Select(p => p.Name));
        var functionName = Qualify(function.Name, ctx.CurrentNamespacePrefix);
        var member = new MemberSymbol
        {
            Name = function.Name,
            Kind = MemberKind.Method,
            ReturnType = function.ReturnType,
            Parameters = function.Parameters.Select(p => new ParameterSymbol(p.Name, p.TypeRef, p.DefaultValue is not null, p.DefaultValue)).ToList(),
            Modifiers = function.Modifiers.ToHashSet(StringComparer.Ordinal)
        };

        ctx.Writer.Write($"function {functionName}({parameters})");
        var previousMember = ctx.CurrentMember;
        ctx.CurrentMember = member;
        ctx.ResetTempVars();
        ctx.Scope.Push();
        foreach (var parameter in function.Parameters)
            ctx.Scope.Declare(parameter.Name, parameter.TypeRef);
        try
        {
            ctx.Dispatch(function.Body, ctx);
        }
        finally
        {
            ctx.Scope.Pop();
            ctx.CurrentMember = previousMember;
        }
    }

    private static string Qualify(string name, string? currentNamespace) =>
        string.IsNullOrEmpty(currentNamespace)
            ? name
            : $"{currentNamespace.Replace(".", "_", StringComparison.Ordinal)}_{name}";
}
