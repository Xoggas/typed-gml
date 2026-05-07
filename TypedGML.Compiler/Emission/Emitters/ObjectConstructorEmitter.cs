using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Ast.Statements;
using TypedGML.Compiler.Symbols;
using TypedGML.Compiler.Utils;

namespace TypedGML.Compiler.Emission.Emitters;

internal sealed class ObjectConstructorEmitter
{
    public void EmitImplicit(EmitContext ctx)
    {
        var objectName = ObjectName(ctx);
        var spatialValues = SpatialValues(null, ctx);
        ctx.Writer.Write($"function {NamingConvention.ConstructorName(ctx.CurrentType!)}()");
        WithConstructorContext(ctx, [], null, () =>
        {
            ctx.ResetTempVars();
            ctx.Writer.BeginBlock();
            ctx.Writer.WriteLine($"var __inst = instance_create_layer({spatialValues[0]}, {spatialValues[1]}, {spatialValues[2]}, {objectName});");
            ctx.Writer.Write("with (__inst)");
            ctx.Writer.BeginBlock();
            ConstructorChainInliner.EmitImplicitBase(ctx.CurrentType?.Base, ctx);
            ctx.Writer.EndBlock();
            ctx.Writer.WriteLine("return __inst;");
            ctx.Writer.EndBlock();
        });
    }

    public void Emit(EmitContext ctx, ConstructorDeclarationNode constructor)
    {
        var parameters = string.Join(", ", constructor.Parameters.Select(p => p.Name));
        var objectName = ObjectName(ctx);
        var symbol = ResolveSymbol(ctx.CurrentType!, constructor);
        var spatialValues = SpatialValues(symbol, ctx);
        var extraParameters = ExtraParameters(symbol, constructor);
        var functionName = NamingConvention.ConstructorName(ctx.CurrentType!);
        ctx.Writer.Write($"function {functionName}({parameters})");
        WithConstructorContext(ctx, constructor.Parameters, symbol, () =>
        {
            ctx.ResetTempVars();
            ctx.Writer.BeginBlock();
            ctx.Writer.WriteLine($"var __inst = instance_create_layer({spatialValues[0]}, {spatialValues[1]}, {spatialValues[2]}, {objectName});");
            if (NeedsInstanceBlock(constructor, extraParameters, ctx))
                EmitInstanceBlock(constructor, extraParameters, ctx);
            ctx.Writer.WriteLine("return __inst;");
            ctx.Writer.EndBlock();
        });
    }

    private static bool NeedsInstanceBlock(ConstructorDeclarationNode constructor, IReadOnlyList<ParameterNode> extraParameters, EmitContext ctx) =>
        extraParameters.Count > 0 ||
        NeedsBaseInitialization(constructor, ctx) ||
        constructor.Body is BlockStatementNode { Statements.Count: > 0 };
    private static bool NeedsBaseInitialization(ConstructorDeclarationNode constructor, EmitContext ctx) =>
        constructor.ChainTarget == ConstructorChainTarget.This ||
        constructor.ChainTarget == ConstructorChainTarget.Base &&
        ctx.CurrentType?.Base?.QualifiedName != "TypedGML.GameObjects.GameObject";
    private static void EmitInstanceBlock(
        ConstructorDeclarationNode constructor,
        IReadOnlyList<ParameterNode> extraParameters,
        EmitContext ctx)
    {
        ctx.Writer.Write("with (__inst)");
        ctx.Writer.BeginBlock();
        ConstructorChainInliner.Emit(constructor, ctx);
        EmitBodyStatements(constructor.Body, ctx);
        EmitParameterAssignments(extraParameters, constructor.Body, ctx);
        ctx.Writer.EndBlock();
    }

    private static string? TargetName(string parameterName, EmitContext ctx) =>
        ctx.CurrentType?.Members.FirstOrDefault(member =>
            member.Kind is MemberKind.Field or MemberKind.Property &&
            !member.Modifiers.Contains("static", StringComparer.Ordinal) &&
            !string.Equals(member.Name, parameterName, StringComparison.Ordinal) &&
            string.Equals(member.Name, parameterName, StringComparison.OrdinalIgnoreCase))?.Name;

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
            if (targetName is not null && !assignedFields.Contains(targetName))
                ctx.Writer.WriteLine($"{targetName} = {parameter.Name};");
        }
    }

    private static MemberSymbol? ResolveSymbol(TypeSymbol type, ConstructorDeclarationNode constructor) =>
        type.Members.FirstOrDefault(member =>
            member.Kind == MemberKind.Constructor &&
            member.Parameters.Select(p => p.TypeRef).SequenceEqual(constructor.Parameters.Select(p => p.TypeRef), StringComparer.Ordinal));

    private static IReadOnlyList<string> SpatialValues(MemberSymbol? symbol, EmitContext ctx) =>
        ObjectConstructorSpatialArguments.TryGetValues(ctx.CurrentType!, symbol, ObjectConstructorLookup.Create(ctx), out var values)
            ? values.Select(arg => ctx.Emitter.Render(arg, ctx)).ToList()
            : ["undefined", "undefined", "undefined"];

    private static string ObjectName(EmitContext ctx) =>
        ctx.CurrentType?.ObjectAssetName ?? ctx.Decorators.ObjectAssetName ?? NamingConvention.TypeName(ctx.CurrentType!);
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
