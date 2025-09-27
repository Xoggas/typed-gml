namespace TypedGML.Transpiler.GameMaker;

public static class PathExtensions
{
    public static string Combine(string path1, string path2)
    {
        return Path.Combine(path1, path2).Replace(Path.DirectorySeparatorChar, '/');
    }
}