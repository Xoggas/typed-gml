namespace TypedGML.CLI.GameMaker;

internal static class YypArrayPropertyReplacer
{
    public static string ReplaceOrAdd(string projectText, string propertyName, string replacement)
    {
        if (new YypTopLevelScanner(projectText).TryFindArrayProperty(propertyName, out var span))
            return projectText[..span.Start] + replacement + projectText[(span.End + 1)..];

        var insertionPoint = projectText.LastIndexOf('}');
        if (insertionPoint < 0)
            return $"{{\n  \"{propertyName}\":{replacement},\n}}\n";

        var prefix = projectText[..insertionPoint].TrimEnd();
        return $"{prefix}\n  \"{propertyName}\":{replacement},\n{projectText[insertionPoint..]}";
    }
}
