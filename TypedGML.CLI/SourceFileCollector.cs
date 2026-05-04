using System.Text.RegularExpressions;

namespace TypedGML.CLI;

internal sealed class SourceFileCollector
{
    public IReadOnlyList<string> Collect(TgmlConfig config)
    {
        if (!Directory.Exists(config.TgmlRoot))
            return [];

        var matchers = config.Sources.Select(pattern => new GlobMatcher(pattern)).ToList();
        return Directory.GetFiles(config.TgmlRoot, "*.tgml", SearchOption.AllDirectories)
            .Where(path => Matches(path, config.TgmlRoot, matchers))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool Matches(string path, string root, IReadOnlyList<GlobMatcher> matchers)
    {
        var relativePath = Path.GetRelativePath(root, path)
            .Replace('\\', '/');
        return matchers.Any(matcher => matcher.IsMatch(relativePath));
    }

    private sealed class GlobMatcher
    {
        private readonly Regex _regex;

        public GlobMatcher(string pattern)
        {
            _regex = new Regex(ToRegex(pattern), RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }

        public bool IsMatch(string relativePath) => _regex.IsMatch(relativePath);

        private static string ToRegex(string pattern)
        {
            var normalized = pattern.Replace('\\', '/').TrimStart('/');
            var regex = new System.Text.StringBuilder("^");
            for (var index = 0; index < normalized.Length; index++)
                AppendToken(normalized, regex, ref index);
            return regex.Append('$').ToString();
        }

        private static void AppendToken(string pattern, System.Text.StringBuilder regex, ref int index)
        {
            if (IsDoubleStarSlash(pattern, index))
            {
                regex.Append("(?:.*/)?");
                index += 2;
                return;
            }

            if (IsDoubleStar(pattern, index))
            {
                regex.Append(".*");
                index++;
                return;
            }

            regex.Append(pattern[index] switch
            {
                '*' => "[^/]*",
                '?' => "[^/]",
                '/' => "/",
                _ => Regex.Escape(pattern[index].ToString())
            });
        }

        private static bool IsDoubleStarSlash(string pattern, int index) =>
            index + 2 < pattern.Length && pattern[index] == '*' && pattern[index + 1] == '*' && pattern[index + 2] == '/';

        private static bool IsDoubleStar(string pattern, int index) =>
            index + 1 < pattern.Length && pattern[index] == '*' && pattern[index + 1] == '*';
    }
}
