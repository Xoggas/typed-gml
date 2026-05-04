namespace TypedGML.CLI;

internal static class UsagePrinter
{
    public static int Print()
    {
        Console.Error.WriteLine("Usage:");
        Console.Error.WriteLine("  typedgml init <yyp-path>");
        Console.Error.WriteLine("  typedgml build [--watch]");
        return 1;
    }
}
