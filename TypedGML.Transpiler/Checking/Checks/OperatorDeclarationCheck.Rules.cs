using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking.Checks;

public sealed partial class OperatorDeclarationCheck
{
    private static void CheckOverloadedOperator(TranspileContext ctx, TgmlFile file, TgmlTypeDecl owner, TgmlMethodDecl method)
    {
        var token = method.OperatorToken!;
        if (method.Params.Count == 1)
        {
            if (!OperatorFacts.SupportsUnary(token))
                ctx.AddError($"Operator '{token}' does not support unary overloading.", file.FileName);
        }
        else if (method.Params.Count == 2)
        {
            if (!OperatorFacts.SupportsBinary(token))
                ctx.AddError($"Operator '{token}' does not support binary overloading.", file.FileName);
        }
        else
        {
            ctx.AddError($"Operator '{token}' must declare either one parameter (unary) or two parameters (binary).", file.FileName);
        }

        if (!method.Params.Any(p => TypeMatchesOwner(p.Type, owner)))
            ctx.AddError($"Operator '{token}' must have at least one parameter of the containing type '{owner.Name}'.", file.FileName);

        if (token is "==" or "!=" or "<" or ">" or "<=" or ">=" or "not" &&
            method.ReturnType.Name.Full != "bool")
        {
            ctx.AddError($"Operator '{token}' must return 'bool'.", file.FileName);
        }
    }

    private static void CheckConversionOperator(TranspileContext ctx, TgmlFile file, TgmlTypeDecl owner, TgmlMethodDecl method)
    {
        if (method.Params.Count != 1)
        {
            ctx.AddError($"Conversion operator '{method.Name}' must declare exactly one parameter.", file.FileName);
            return;
        }

        var sourceType = method.Params[0].Type;
        var targetType = method.ReturnType;
        var sourceMatchesOwner = TypeMatchesOwner(sourceType, owner);
        var targetMatchesOwner = TypeMatchesOwner(targetType, owner);

        if (!sourceMatchesOwner && !targetMatchesOwner)
            ctx.AddError($"Conversion operator '{method.Name}' must convert from or to the containing type '{owner.Name}'.", file.FileName);
        if (DefaultExpressionFacts.DescribeType(sourceType) == DefaultExpressionFacts.DescribeType(targetType))
            ctx.AddError($"Conversion operator '{method.Name}' must convert between different types.", file.FileName);
        if (IsInterfaceLike(sourceType, ctx) || IsInterfaceLike(targetType, ctx))
            ctx.AddError($"Conversion operator '{method.Name}' cannot convert to or from an interface type.", file.FileName);
        if (CrossesClassStructBoundary(sourceType, targetType, ctx))
            ctx.AddError($"Conversion operator '{method.Name}' cannot convert between class and struct types.", file.FileName);
    }

    private static bool TypeMatchesOwner(TgmlTypeRef typeRef, TgmlTypeDecl owner)
    {
        var typeName = typeRef.Name.Full;
        var ownerName = owner.QualifiedName ?? owner.Name;
        return typeName == ownerName ||
               typeName == owner.Name ||
               TypeCompatibility.ArePrimitiveEquivalent(typeName, ownerName) ||
               TypeCompatibility.ArePrimitiveEquivalent(typeName, owner.Name);
    }

    private static bool IsInterfaceLike(TgmlTypeRef typeRef, TranspileContext ctx)
        => ctx.TypeTable.TryResolve(typeRef.Name.Full, out var decl) && decl is TgmlInterfaceDecl;

    private static bool CrossesClassStructBoundary(TgmlTypeRef sourceType, TgmlTypeRef targetType, TranspileContext ctx)
    {
        if (!ctx.TypeTable.TryResolve(sourceType.Name.Full, out var sourceDecl) ||
            !ctx.TypeTable.TryResolve(targetType.Name.Full, out var targetDecl) ||
            sourceDecl is null ||
            targetDecl is null)
        {
            return false;
        }

        return sourceDecl switch
        {
            TgmlClassDecl => targetDecl is TgmlStructDecl,
            TgmlStructDecl => targetDecl is TgmlClassDecl,
            _ => false
        };
    }
}
