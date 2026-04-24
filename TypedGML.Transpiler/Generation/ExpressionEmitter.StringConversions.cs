using TypedGML.Transpiler.Checking;
using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Generation;

public sealed partial class ExpressionEmitter
{
    private bool TryEmitResolvedStringLiteral(TgmlExpression expr, out string emitted)
    {
        if (expr.Metadata.TryGetValue(ObjectFacts.ResolvedStringLiteralConversionMetadata, out var literalValue) &&
            literalValue is string literal)
        {
            emitted = QuoteString(literal);
            return true;
        }

        emitted = string.Empty;
        return false;
    }

    private static string QuoteString(string value)
        => $"\"{value.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";
}
