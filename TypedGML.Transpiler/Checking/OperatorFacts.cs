using System.Text;
using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking;

public static class OperatorFacts
{
    public static bool SupportsUnary(string token) => token is "+" or "-" or "~" or "not";

    public static bool SupportsBinary(string token) => token is
        "+" or "-" or "*" or "/" or "%" or
        "&" or "|" or "^" or "<<" or ">>" or
        "==" or "!=" or "<" or ">" or "<=" or ">=";

    public static string GetSyntheticMethodName(TgmlMethodDecl method)
    {
        if (method.IsConversionOperator)
        {
            var kind = method.Conversion == ConversionModifier.Implicit ? "implicit" : "explicit";
            return $"operator {kind} {method.ReturnType}";
        }

        return $"operator {method.OperatorToken}";
    }

    public static string GetHelperName(TgmlTypeDecl owner, TgmlMethodDecl method)
    {
        var ownerName = (owner.QualifiedName ?? owner.Name).Replace(".", "_");
        var signature = string.Join("_", method.Params.Select(p => SanitizeIdentifier(p.Type.ToString())));

        if (method.IsConversionOperator)
        {
            var kind = method.Conversion == ConversionModifier.Implicit ? "implicit" : "explicit";
            return $"{ownerName}__conv_{kind}_{SanitizeIdentifier(method.ReturnType.ToString())}_{signature}";
        }

        var operatorName = method.OperatorToken ?? method.Name;
        return $"{ownerName}__op_{SanitizeIdentifier(operatorName)}_{signature}";
    }

    public static string SanitizeIdentifier(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            builder.Append(char.IsLetterOrDigit(ch) || ch == '_' ? ch : '_');
        }

        return builder.ToString();
    }
}
