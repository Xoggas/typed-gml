namespace TypedGML.CLI.GameMaker;

internal readonly record struct YypResourceArraySpan(int Start, int End)
{
    public static bool TryFind(string text, out YypResourceArraySpan span)
    {
        span = default;
        var scanner = new YypTopLevelScanner(text);
        return scanner.TryFindArrayProperty("resources", out span);
    }
}
