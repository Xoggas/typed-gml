using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Emission.Emitters;

internal sealed class ObjectConstructorEmitter
{
    public void Emit(EmitContext ctx, ConstructorDeclarationNode constructor)
    {
        var parameters = string.Join(", ", constructor.Parameters.Select(p => p.Name));
        var objectName = ctx.CurrentType?.ObjectAssetName ?? ctx.Decorators.ObjectAssetName ?? NamingConvention.TypeName(ctx.CurrentType!);
        var x = constructor.Parameters.ElementAtOrDefault(0)?.Name ?? "x";
        var y = constructor.Parameters.ElementAtOrDefault(1)?.Name ?? "y";
        var layer = constructor.Parameters.ElementAtOrDefault(2)?.Name ?? "layer";

        var symbol = ResolveSymbol(ctx.CurrentType!, constructor);
        var functionName = symbol is null
            ? NamingConvention.ConstructorName(ctx.CurrentType!)
            : NamingConvention.ConstructorName(ctx.CurrentType!, symbol);
        ctx.Writer.Write($"function {functionName}({parameters})");
        ctx.Writer.BeginBlock();
        if (constructor.Parameters.Count <= 3)
            ctx.Writer.WriteLine($"return instance_create_layer({x}, {y}, {layer}, {objectName});");
        else
        {
            ctx.Writer.WriteLine($"var __inst = instance_create_layer({x}, {y}, {layer}, {objectName});");
            ctx.Writer.Write("with (__inst)");
            ctx.Writer.BeginBlock();
            foreach (var parameter in constructor.Parameters.Skip(3))
                ctx.Writer.WriteLine($"{TargetName(parameter.Name, ctx)} = {parameter.Name};");
            ctx.Writer.EndBlock();
            ctx.Writer.WriteLine("return __inst;");
        }

        ctx.Writer.EndBlock();
    }

    private static string TargetName(string parameterName, EmitContext ctx)
    {
        var member = ctx.CurrentType?.Members.FirstOrDefault(member =>
            member.Kind is MemberKind.Field or MemberKind.Property &&
            !member.Modifiers.Contains("static", StringComparer.Ordinal) &&
            string.Equals(member.Name, parameterName, StringComparison.OrdinalIgnoreCase));

        return member?.Name ?? parameterName;
    }

    private static MemberSymbol? ResolveSymbol(TypeSymbol type, ConstructorDeclarationNode constructor) =>
        type.Members.FirstOrDefault(member =>
            member.Kind == MemberKind.Constructor &&
            member.Parameters.Select(p => p.TypeRef).SequenceEqual(constructor.Parameters.Select(p => p.TypeRef), StringComparer.Ordinal));
}
