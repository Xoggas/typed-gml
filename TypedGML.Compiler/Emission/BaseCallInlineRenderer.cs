using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Ast.Statements;
using TypedGML.Compiler.Ast.Declarations;
using TypedGML.Compiler.Emission.Emitters.Expressions;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Emission;

internal static class BaseCallInlineRenderer
{
    public static string Render(BaseCallExpressionNode call, EmitContext ctx)
    {
        var method = ResolveMethod(call, ctx);
        if (method?.Method.Body is null)
            return "undefined";

        var writer = new GmlWriter();
        var nested = ctx.WithWriter(writer);
        writer.Write("(function()");
        writer.BeginBlock();
        EmitResolved(call, method.Value.Owner, method.Value.Method, nested);
        writer.EndBlock();
        return $"{writer.GetOutput().TrimEnd()})()";
    }

    public static void Emit(BaseCallExpressionNode call, EmitContext ctx)
    {
        var method = ResolveMethod(call, ctx);
        if (method?.Method.Body is null)
            return;

        EmitResolved(call, method.Value.Owner, method.Value.Method, ctx);
    }

    private static void EmitResolved(BaseCallExpressionNode call, TypeSymbol owner, MethodDeclarationNode method, EmitContext ctx)
    {
        var previousBaseLookup = ctx.BaseCallLookupType;
        ctx.BaseCallLookupType = owner.Base;
        ctx.Scope.Push();
        ctx.PushSubstitution(Substitutions(method, call));
        try
        {
            foreach (var parameter in method.Parameters)
                ctx.Scope.Declare(parameter.Name, parameter.TypeRef);

            if (method.Body is BlockStatementNode block)
                foreach (var statement in block.Statements)
                    ctx.Emitter.Emit(statement, ctx);
            else if (method.Body is not null)
                ctx.Emitter.Emit(method.Body, ctx);
        }
        finally
        {
            ctx.PopSubstitution();
            ctx.Scope.Pop();
            ctx.BaseCallLookupType = previousBaseLookup;
        }
    }

    private static (TypeSymbol Owner, MethodDeclarationNode Method)? ResolveMethod(BaseCallExpressionNode call, EmitContext ctx)
    {
        for (var current = ctx.BaseCallLookupType ?? ctx.CurrentType?.Base; current is not null; current = current.Base)
        {
            var symbol = EmissionOverloadResolver.Pick(
                current.Members.Where(member => member.Kind == MemberKind.Method && member.Name == call.MemberName).ToList(),
                call.Args,
                [],
                ctx);
            var method = FindDeclaration(current, symbol, ctx) ?? FindDeclaration(current, call, ctx);
            if (method is not null)
                return (current, method);
        }

        return null;
    }

    private static MethodDeclarationNode? FindDeclaration(TypeSymbol type, MemberSymbol? symbol, EmitContext ctx)
    {
        if (symbol is null ||
            !TryGetClass(type, ctx, out var @class))
            return null;

        return @class.Members.OfType<MethodDeclarationNode>()
            .FirstOrDefault(method =>
                method.Name == symbol.Name &&
                method.Parameters.Select(p => p.TypeRef).SequenceEqual(symbol.Parameters.Select(p => p.TypeRef), StringComparer.Ordinal));
    }

    private static MethodDeclarationNode? FindDeclaration(TypeSymbol type, BaseCallExpressionNode call, EmitContext ctx)
    {
        if (!TryGetClass(type, ctx, out var @class))
            return null;

        return @class.Members.OfType<MethodDeclarationNode>()
            .FirstOrDefault(method => method.Name == call.MemberName && method.Parameters.Count == call.Args.Count);
    }

    private static bool TryGetClass(TypeSymbol type, EmitContext ctx, out ClassDeclarationNode declaration)
    {
        declaration = null!;
        if (!ctx.TypeDeclarations.TryGetValue(TypeDeclarationMapBuilder.Key(type), out var node))
            return false;

        if (node is not ClassDeclarationNode @class)
            return false;

        declaration = @class;
        return true;
    }

    private static Dictionary<string, IAstNode> Substitutions(MethodDeclarationNode method, BaseCallExpressionNode call)
    {
        var map = new Dictionary<string, IAstNode>(StringComparer.Ordinal);
        for (var i = 0; i < method.Parameters.Count; i++)
        {
            var value = i < call.Args.Count ? call.Args[i] : method.Parameters[i].DefaultValue;
            if (value is not null)
                map[method.Parameters[i].Name] = value;
        }

        return map;
    }
}
