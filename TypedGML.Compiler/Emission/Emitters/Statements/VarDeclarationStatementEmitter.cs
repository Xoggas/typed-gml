using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Ast.Statements;
using TypedGML.Compiler.Emission.Emitters.Expressions;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Emission.Emitters.Statements;

public sealed class VarDeclarationStatementEmitter : INodeEmitter
{
    public bool Matches(IAstNode node) => node is VarDeclarationStatementNode;

    public void Emit(IAstNode node, EmitContext ctx)
    {
        var statement = (VarDeclarationStatementNode)node;
        if (TryEmitBaseCallInitializer(statement, ctx))
            return;

        if (TryEmitGenericObjectCreation(statement, ctx))
            return;

        var initializer = Initializer(statement, ctx);
        ctx.Writer.WriteLine($"var {statement.Name}{initializer};");
        var typeRef = statement.TypeRef ?? ExpressionTypeLookup.Resolve(statement.Initializer, ctx);
        if (!string.IsNullOrWhiteSpace(typeRef))
            ctx.Scope.Declare(statement.Name, typeRef);
    }

    private static string Initializer(VarDeclarationStatementNode statement, EmitContext ctx)
    {
        if (statement.Initializer is not null)
            return $" = {RenderInitializerExpression(statement, ctx)}";

        return IsDelegateType(statement.TypeRef, ctx) ? " = []" : string.Empty;
    }

    internal static string RenderInitializerExpression(VarDeclarationStatementNode statement, EmitContext ctx) =>
        string.IsNullOrWhiteSpace(statement.TypeRef)
            ? ctx.Emitter.Render(statement.Initializer, ctx)
            : ctx.RenderWithExpected(statement.Initializer!, statement.TypeRef);

    private static bool TryEmitBaseCallInitializer(VarDeclarationStatementNode statement, EmitContext ctx)
    {
        if (statement.Initializer is not BaseCallExpressionNode baseCall)
            return false;

        const string resultName = "__base_result";
        ctx.Writer.WriteLine($"var {resultName};");
        BaseCallInlineRenderer.EmitWithReturnTarget(baseCall, resultName, ctx);
        ctx.Writer.WriteLine($"var {statement.Name} = {resultName};");
        var typeRef = statement.TypeRef ?? ExpressionTypeLookup.Resolve(statement.Initializer, ctx);
        if (!string.IsNullOrWhiteSpace(typeRef))
            ctx.Scope.Declare(statement.Name, typeRef);
        return true;
    }

    private static bool IsDelegateType(string? typeRef, EmitContext ctx) =>
        !string.IsNullOrWhiteSpace(typeRef) &&
        ExpressionSymbolHelper.TryResolveType(ctx, typeRef, out var symbol) &&
        symbol.Kind == TypeKind.Delegate;

    private static bool TryEmitGenericObjectCreation(VarDeclarationStatementNode statement, EmitContext ctx)
    {
        if (statement.Initializer is not ObjectCreationExpressionNode creation ||
            creation.TypeArgs.Count == 0 ||
            !ExpressionSymbolHelper.TryResolveType(ctx, creation.TypeRef, out var type) ||
            type.GenericParameters.Count == 0)
            return false;

        var args = ExpressionCallHelper.JoinConstructorArguments(type, creation.PositionalArgs, creation.NamedArgs, ctx);
        ctx.Writer.WriteLine($"var {statement.Name} = {ConstructorName(type, creation, ctx)}({args});");
        ctx.Writer.WriteLine($"{statement.Name}.__genericArgs = {GenericArgsRenderer.Render(type, creation.TypeArgs, ctx)};");
        ctx.Scope.Declare(statement.Name, statement.TypeRef ?? creation.TypeRef);
        return true;
    }

    private static string ConstructorName(TypeSymbol type, ObjectCreationExpressionNode creation, EmitContext ctx)
    {
        var constructor = EmissionOverloadResolver.Pick(
            type.Members.Where(member => member.Kind == MemberKind.Constructor).ToList(),
            creation.PositionalArgs,
            creation.NamedArgs,
            ctx,
            type.GenericParameters.Zip(creation.TypeArgs)
                .ToDictionary(pair => pair.First.Name, pair => pair.Second, StringComparer.Ordinal));

        return constructor is null
            ? NamingConvention.ConstructorName(type)
            : NamingConvention.ConstructorName(type, constructor);
    }
}
