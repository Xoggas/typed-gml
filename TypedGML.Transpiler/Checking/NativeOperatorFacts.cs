using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking;

public static class NativeOperatorFacts
{
    public const string DecoratorName = "NativeOperator";

    public static bool IsNativeOperator(TgmlMethodDecl method)
        => TryGetOperatorToken(method, out _);

    public static bool Matches(TgmlMethodDecl method, string typedGmlOperator)
        => TryGetOperatorToken(method, out var token) &&
           string.Equals(token, typedGmlOperator, StringComparison.Ordinal);

    public static bool TryGetOperatorToken(TgmlMethodDecl method, out string operatorToken)
    {
        operatorToken = string.Empty;

        var decoratorValue = method.GetDecorator(DecoratorName)?.GetFirstStringArg();
        if (string.IsNullOrWhiteSpace(decoratorValue))
            return false;

        operatorToken = NormalizeTypedToken(decoratorValue);
        return true;
    }

    public static string ToGmlToken(string typedGmlOperator) => typedGmlOperator switch
    {
        "not" => "!",
        "and" => "&&",
        "or" => "||",
        _ => typedGmlOperator
    };

    private static string NormalizeTypedToken(string token) => token switch
    {
        "!" => "not",
        "&&" => "and",
        "||" => "or",
        _ => token
    };
}
