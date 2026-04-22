using Antlr4.Runtime;
using TypedGML.Transpiler.Population.Models;
using TypedGML.Transpiler.Visitor;

namespace TypedGML.Transpiler.Population;

/// <summary>
///     Entry point for the Population stage.
///     Parses <c>.tgml</c> source files using ANTLR4 and returns typed AST models.
/// </summary>
public static class Populator
{
    /// <summary>
    ///     Parses all <paramref name="sources"/> and returns the resulting AST files
    ///     together with any parse diagnostics.
    /// </summary>
    /// <param name="sources">The source files to parse.</param>
    /// <returns>
    ///     A tuple of the populated <see cref="TgmlFile"/> list and any
    ///     <see cref="TranspileDiagnostic"/> entries produced during parsing.
    /// </returns>
    public static (List<TgmlFile> Files, List<TranspileDiagnostic> Diagnostics)
        Populate(IReadOnlyList<TgmlSourceFile> sources)
    {
        var files       = new List<TgmlFile>();
        var diagnostics = new List<TranspileDiagnostic>();

        foreach (var source in sources)
        {
            try
            {
                var (file, fileDiags) = ParseFile(source);

                // Assign the source file name and compute qualified names for all top-level types.
                var ns = file.PrimaryNamespace;
                foreach (var t in file.TypeDecls)
                    t.QualifiedName ??= string.IsNullOrEmpty(ns) ? t.Name : $"{ns}.{t.Name}";

                var namedFile = new TgmlFile
                {
                    FileName  = source.FileName,
                    Usings    = file.Usings,
                    Namespaces = file.Namespaces,
                    TypeDecls = file.TypeDecls
                };

                files.Add(namedFile);
                diagnostics.AddRange(fileDiags);
            }
            catch (Exception ex)
            {
                diagnostics.Add(new TranspileDiagnostic(
                    DiagnosticSeverity.Error,
                    $"Failed to parse file: {ex.Message}",
                    source.FileName));
            }
        }

        return (files, diagnostics);
    }

    // ── Internal helpers ──────────────────────────────────────────────────────

    private static (TgmlFile File, List<TranspileDiagnostic> Diagnostics)
        ParseFile(TgmlSourceFile source)
    {
        var diagnostics = new List<TranspileDiagnostic>();

        var inputStream = new AntlrInputStream(source.Content);

        var lexer = new TypedGMLLexer(inputStream);
        lexer.RemoveErrorListeners();
        lexer.AddErrorListener(new LexerErrorListener(source.FileName, diagnostics));

        var tokenStream = new CommonTokenStream(lexer);
        var parser      = new TypedGMLParser(tokenStream);
        parser.RemoveErrorListeners();
        parser.AddErrorListener(new ParserErrorListener(source.FileName, diagnostics));

        var tree    = parser.program();
        var visitor = new AstVisitor();
        var file    = (TgmlFile)visitor.Visit(tree)!;

        return (file, diagnostics);
    }
}