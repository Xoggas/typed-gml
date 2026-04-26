namespace TypedGML.Compiler.Emission.Emitters;

internal static class IntrinsicOpEmitter
{
    public static bool TryEmit(string nativeCallName, string[] argNames, string returnType, GmlWriter writer)
    {
        var expressionWriter = new GmlWriter();
        if (!TryEmitIntrinsicOp(nativeCallName, argNames, expressionWriter))
            return false;

        var expression = expressionWriter.GetOutput();
        if (string.Equals(returnType, "void", StringComparison.Ordinal))
            writer.WriteLine($"{expression};");
        else
            writer.WriteLine($"return {expression};");
        return true;
    }

    private static bool TryEmitIntrinsicOp(string nativeCallName, string[] argNames, GmlWriter writer)
    {
        switch (nativeCallName)
        {
            case "__op_add":
                return Binary("+", argNames, writer);
            case "__op_sub":
                return Binary("-", argNames, writer);
            case "__op_mul":
                return Binary("*", argNames, writer);
            case "__op_div":
                return Binary("/", argNames, writer);
            case "__op_mod":
                return Binary("%", argNames, writer);
            case "__op_neg":
                return Prefix("-", argNames, writer);
            case "__op_bitnot":
                return Prefix("~", argNames, writer);
            case "__op_eq":
                return Binary("==", argNames, writer);
            case "__op_neq":
                return Binary("!=", argNames, writer);
            case "__op_lt":
                return Binary("<", argNames, writer);
            case "__op_gt":
                return Binary(">", argNames, writer);
            case "__op_lte":
                return Binary("<=", argNames, writer);
            case "__op_gte":
                return Binary(">=", argNames, writer);
            case "__op_str_num_add":
                if (argNames.Length != 2)
                    return false;
                writer.Write($"{argNames[0]} + string({argNames[1]})");
                return true;
            case "__op_num_str_add":
                if (argNames.Length != 2)
                    return false;
                writer.Write($"string({argNames[0]}) + {argNames[1]}");
                return true;
            default:
                return false;
        }
    }

    private static bool Binary(string op, string[] argNames, GmlWriter writer)
    {
        if (argNames.Length != 2)
            return false;

        writer.Write($"{argNames[0]} {op} {argNames[1]}");
        return true;
    }

    private static bool Prefix(string op, string[] argNames, GmlWriter writer)
    {
        if (argNames.Length != 1)
            return false;

        writer.Write($"{op}{argNames[0]}");
        return true;
    }
}
