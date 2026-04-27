using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Ast.Statements;
using TypedGML.Compiler.Ast.Declarations;

namespace TypedGML.Compiler.Emission;

internal static class BaseCallInlineRenderer
{
    public static string Render(BaseCallExpressionNode call, EmitContext ctx)
    {
        var method = ResolveMethod(call, ctx);
        if (method?.Body is null)
            return $"{call.MemberName}({string.Join(", ", call.Args.Select(a => ctx.Emitter.Render(a, ctx)))})";

        var writer = new GmlWriter();
        var nested = ctx.WithWriter(writer);
        nested.Scope.Push();
        try
        {
            foreach (var parameter in method.Parameters)
                nested.Scope.Declare(parameter.Name, parameter.TypeRef);

            writer.Write($"(function({string.Join(", ", method.Parameters.Select(p => p.Name))})");
            if (method.Body is BlockStatementNode block)
            {
                writer.BeginBlock();
                foreach (var statement in block.Statements)
                    nested.Emitter.Emit(statement, nested);
                writer.EndBlock();
            }
            else
            {
                writer.BeginBlock();
                nested.Emitter.Emit(method.Body, nested);
                writer.EndBlock();
            }

            return $"{writer.GetOutput().TrimEnd()}({string.Join(", ", call.Args.Select(a => ctx.Emitter.Render(a, ctx)))})";
        }
        finally
        {
            nested.Scope.Pop();
        }
    }

    private static MethodDeclarationNode? ResolveMethod(BaseCallExpressionNode call, EmitContext ctx)
    {
        for (var current = ctx.CurrentType?.Base; current is not null; current = current.Base)
            if (ctx.TypeDeclarations.TryGetValue(current.QualifiedName, out var declaration) && declaration is ClassDeclarationNode @class)
            {
                var match = @class.Members.OfType<MethodDeclarationNode>()
                    .FirstOrDefault(method => method.Name == call.MemberName && method.Parameters.Count == call.Args.Count);
                if (match is not null)
                    return match;
            }

        return null;
    }
}
