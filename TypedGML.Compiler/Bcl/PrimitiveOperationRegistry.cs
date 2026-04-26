namespace TypedGML.Compiler.Bcl;

public static class PrimitiveOperationRegistry
{
    public static string? ResolveResultType(string op, string leftType, string rightType) =>
        op switch
        {
            "+" when leftType == "number" && rightType == "number" => "number",
            "+" when leftType == "string" && rightType == "string" => "string",
            "+" when leftType == "string" && rightType == "number" => "string",
            "+" when leftType == "number" && rightType == "string" => "string",
            "-" or "*" or "/" or "%" when leftType == "number" && rightType == "number" => "number",
            "<" or ">" or "<=" or ">=" when leftType == "number" && rightType == "number" => "bool",
            "==" when IsEqualityAllowed(leftType, rightType) => "bool",
            "!=" when IsEqualityAllowed(leftType, rightType) => "bool",
            "and" or "or" when leftType == "bool" && rightType == "bool" => "bool",
            _ => null
        };

    public static string? ResolveUnaryResultType(string op, string operandType) =>
        op switch
        {
            "not" when operandType == "bool" => "bool",
            "-" when operandType == "number" => "number",
            "~" when operandType == "number" => "number",
            _ => null
        };

    private static bool IsEqualityAllowed(string leftType, string rightType) =>
        leftType == rightType || leftType == "null" || rightType == "null";
}
