namespace TypedGML.Transpiler.GameMaker.IO;

public static class PathExtensions
{
    public static string Combine(params string[] paths)
    {
        return Path.Combine(paths).Replace(Path.DirectorySeparatorChar, '/');
    }
}