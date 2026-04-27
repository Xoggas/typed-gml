using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Statements;
using TypedGML.Compiler.Emission.Emitters.Expressions;

namespace TypedGML.Compiler.Emission.Emitters.Statements;

public sealed class VarDeclarationStatementEmitter : INodeEmitter
{
    public bool Matches(IAstNode node) => node is VarDeclarationStatementNode;

    public void Emit(IAstNode node, EmitContext ctx)
    {
        var statement = (VarDeclarationStatementNode)node;
        var initializer = statement.Initializer is null ? string.Empty : $" = {ctx.Emitter.Render(statement.Initializer, ctx)}";
        ctx.Writer.WriteLine($"var {statement.Name}{initializer};");
        var typeRef = statement.TypeRef ?? ExpressionTypeLookup.Resolve(statement.Initializer, ctx);
        if (!string.IsNullOrWhiteSpace(typeRef))
            ctx.Scope.Declare(statement.Name, typeRef);
    }
}
