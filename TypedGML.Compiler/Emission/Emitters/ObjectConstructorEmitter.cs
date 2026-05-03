using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Ast.Statements;
using TypedGML.Compiler.Emission.Emitters.Expressions;
using TypedGML.Compiler.Symbols;
using TypedGML.Compiler.Utils;

namespace TypedGML.Compiler.Emission.Emitters;

internal sealed class ObjectConstructorEmitter
{
    public void Emit(EmitContext ctx, ConstructorDeclarationNode constructor)
    {
        var parameters = string.Join(", ", constructor.Parameters.Select(p => p.Name));
        var objectName = ctx.CurrentType?.ObjectAssetName ?? ctx.Decorators.ObjectAssetName ?? NamingConvention.TypeName(ctx.CurrentType!);
        var symbol = ResolveSymbol(ctx.CurrentType!, constructor);
        var spatialValues = SpatialValues(symbol, constructor, ctx);
        var extraParameters = ExtraParameters(symbol, constructor);
        var functionName = symbol is null
            ? NamingConvention.ConstructorName(ctx.CurrentType!)
            : NamingConvention.ConstructorName(ctx.CurrentType!, symbol);
        ctx.Writer.Write($"function {functionName}({parameters})");
        WithConstructorContext(ctx, constructor.Parameters, symbol, () =>
        {
            ctx.ResetTempVars();
            ctx.Writer.BeginBlock();
            if (!NeedsInstanceBlock(constructor, extraParameters))
            {
                ctx.Writer.WriteLine($"return instance_create_layer({spatialValues[0]}, {spatialValues[1]}, {spatialValues[2]}, {objectName});");
                ctx.Writer.EndBlock();
                return;
            }

            ctx.Writer.WriteLine($"var __inst = instance_create_layer({spatialValues[0]}, {spatialValues[1]}, {spatialValues[2]}, {objectName});");
            ctx.Writer.Write("with (__inst)");
            ctx.Writer.BeginBlock();
            ConstructorChainInliner.Emit(constructor, ctx);
            EmitBodyStatements(constructor.Body, ctx);
            EmitParameterAssignments(extraParameters, constructor.Body, ctx);
            ctx.Writer.EndBlock();
            ctx.Writer.WriteLine("return __inst;");
            ctx.Writer.EndBlock();
        });
    }

    private static bool NeedsInstanceBlock(ConstructorDeclarationNode constructor, IReadOnlyList<ParameterNode> extraParameters) =>
        extraParameters.Count > 0 ||
        constructor.ChainTarget != ConstructorChainTarget.None ||
        constructor.Body is BlockStatementNode { Statements.Count: > 0 };

    private static string TargetName(string parameterName, EmitContext ctx)
    {
        var member = ctx.CurrentType?.Members.FirstOrDefault(member =>
            member.Kind is MemberKind.Field or MemberKind.Property &&
            !member.Modifiers.Contains("static", StringComparer.Ordinal) &&
            string.Equals(member.Name, parameterName, StringComparison.OrdinalIgnoreCase));

        return member?.Name ?? parameterName;
    }

    private static void EmitBodyStatements(IAstNode body, EmitContext ctx)
    {
        if (body is BlockStatementNode block)
        {
            foreach (var statement in block.Statements)
                ctx.Dispatch(statement, ctx);
            return;
        }

        ctx.Dispatch(body, ctx);
    }

    private static void EmitParameterAssignments(IReadOnlyList<ParameterNode> extraParameters, IAstNode body, EmitContext ctx)
    {
        var assignedFields = ConstructorFieldAssignmentFinder.Find(body);
        foreach (var parameter in extraParameters)
        {
            var targetName = TargetName(parameter.Name, ctx);
            if (!assignedFields.Contains(targetName))
                ctx.Writer.WriteLine($"{targetName} = {parameter.Name};");
        }
    }

    private static MemberSymbol? ResolveSymbol(TypeSymbol type, ConstructorDeclarationNode constructor) =>
        type.Members.FirstOrDefault(member =>
            member.Kind == MemberKind.Constructor &&
            member.Parameters.Select(p => p.TypeRef).SequenceEqual(constructor.Parameters.Select(p => p.TypeRef), StringComparer.Ordinal));

    private static IReadOnlyList<string> SpatialValues(MemberSymbol? symbol, ConstructorDeclarationNode constructor, EmitContext ctx) =>
        ObjectConstructorSpatialArguments.TryGetBaseChainValues(symbol, arg => ExpressionTypeLookup.Resolve(arg, ctx), out var values)
            ? values.Select(arg => ctx.Emitter.Render(arg, ctx)).ToList()
            : [
                constructor.Parameters.ElementAtOrDefault(0)?.Name ?? "x",
                constructor.Parameters.ElementAtOrDefault(1)?.Name ?? "y",
                constructor.Parameters.ElementAtOrDefault(2)?.Name ?? "layer"
            ];

    private static IReadOnlyList<ParameterNode> ExtraParameters(MemberSymbol? symbol, ConstructorDeclarationNode constructor) =>
        symbol is not null && ObjectConstructorSpatialArguments.ParametersSupplyRequiredValues(symbol)
            ? constructor.Parameters.Skip(3).ToList()
            : constructor.Parameters;

    private static void WithConstructorContext(
        EmitContext ctx,
        IReadOnlyList<ParameterNode> parameters,
        MemberSymbol? symbol,
        Action action)
    {
        var previousSelf = ctx.SelfName;
        var previousMember = ctx.CurrentMember;
        var previousConstructor = ctx.IsInConstructor;
        var previousObjectMethod = ctx.IsInObjectMethod;
        ctx.SelfName = "self";
        ctx.CurrentMember = symbol;
        ctx.IsInConstructor = true;
        ctx.IsInObjectMethod = true;
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
            ctx.IsInConstructor = previousConstructor;
            ctx.IsInObjectMethod = previousObjectMethod;
            ctx.CurrentMember = previousMember;
            ctx.SelfName = previousSelf;
        }
    }
}
