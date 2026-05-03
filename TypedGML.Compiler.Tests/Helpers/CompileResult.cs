using System.Text;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Tests.Helpers;

public sealed class CompileResult
{
    public CompileResult(
        IReadOnlyList<Diagnostic> errors,
        IReadOnlyList<Diagnostic> warnings,
        IReadOnlyDictionary<string, string> output,
        IReadOnlyList<string>? namespaces = null)
    {
        Errors = errors;
        Warnings = warnings;
        Output = output;
        Namespaces = namespaces ?? [];
    }

    public bool HasErrors => Errors.Count > 0;

    public bool HasWarnings => Warnings.Count > 0;

    public IReadOnlyList<Diagnostic> Errors { get; }

    public IReadOnlyList<Diagnostic> Warnings { get; }

    public IReadOnlyDictionary<string, string> Output { get; }

    public IReadOnlyList<string> Namespaces { get; }

    public bool HasError(DiagnosticCode code) =>
        Errors.Any(error => error.Code == code);

    public string? GetFile(string filePathSuffix)
    {
        var suffix = NormalizePath(filePathSuffix);
        var match = Output.FirstOrDefault(entry =>
            NormalizePath(entry.Key).EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
        return match.Equals(default(KeyValuePair<string, string>)) ? null : match.Value;
    }

    public string AllOutput()
    {
        var builder = new StringBuilder();
        foreach (var content in Output.OrderBy(entry => entry.Key, StringComparer.Ordinal).Select(entry => entry.Value))
            builder.AppendLine(content);
        return builder.ToString();
    }

    private static string NormalizePath(string path) =>
        path.Replace('\\', '/');
}
