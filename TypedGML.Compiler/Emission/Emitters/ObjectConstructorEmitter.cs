using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Symbols;
using TypedGML.Compiler.Utils;

namespace TypedGML.Compiler.Emission.Emitters;

internal sealed class ObjectConstructorEmitter
{
    private readonly ObjectConstructorParameterEmitter _parameterEmitter = new();
    private readonly ObjectConstructorBodyEmitter _bodyEmitter = new();

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
            EmitInstanceBlock(constructor, extraParameters, ctx);
            ctx.Writer.WriteLine("return __inst;");
            ctx.Writer.EndBlock();
        });
    }

    private void EmitInstanceBlock(
        ConstructorDeclarationNode constructor,
        IReadOnlyList<ParameterNode> extraParameters,
        EmitContext ctx)
    {
        var assignedMembers = ConstructorFieldAssignmentFinder.Find(constructor.Body);
        if (!_bodyEmitter.NeedsBody(constructor, ctx) &&
            !_parameterEmitter.HasAssignments(extraParameters, assignedMembers, ctx))
            return;

        ctx.Writer.Write("with (__inst)");
        ctx.Writer.BeginBlock();
        _bodyEmitter.Emit(constructor, ctx);
        _parameterEmitter.Emit(extraParameters, assignedMembers, ctx);
        ctx.Writer.EndBlock();
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
