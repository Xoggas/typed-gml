using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Diagnostics;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Verification;

internal static class ConstantExpressionHelper
{
    public static bool IsConstant(IAstNode? node, VerificationContext ctx) =>
        TryValue(node, ctx, new HashSet<string>(StringComparer.Ordinal), out _);

    public static bool TryValue(IAstNode? node, VerificationContext ctx, ISet<string> stack, out object? value)
    {
        switch (node)
        {
            case LiteralExpressionNode literal:
                value = literal.Value;
                return true;
            case IdentifierExpressionNode identifier:
                return TryConstMember(identifier.Name, ctx.CurrentType, ctx, stack, out value);
            case MemberAccessExpressionNode access when access.Target is IdentifierExpressionNode target:
                if (SymbolResolver.TryResolveType(target.Name, ctx, out var type))
                    return TryConstMember(access.MemberName, type, ctx, stack, out value);
                break;
        }

        value = null;
        return false;
    }

    private static bool TryConstMember(string name, TypeSymbol? type, VerificationContext ctx, ISet<string> stack, out object? value)
    {
        var member = SymbolResolver.FindMember(type, name, out _);
        if (member is null || !member.Modifiers.Contains("const", StringComparer.Ordinal))
        {
            value = null;
            return false;
        }

        if (!stack.Add($"{type?.QualifiedName}.{name}"))
        {
            value = null;
            return false;
        }

        value = member.ConstValue ?? name;
        return true;
    }
}
