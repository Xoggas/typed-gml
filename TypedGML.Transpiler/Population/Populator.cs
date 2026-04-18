using Antlr4.Runtime;
using TypedGML.Transpiler.Population.Models;
using TypedGML.Transpiler.Visitor;

namespace TypedGML.Transpiler.Population;

/// <summary>
///     Entry point for the Population stage.
///     Parses .tgml source files using ANTLR and returns typed AST models.
/// </summary>
public static class Populator
{
    public static (List<TgmlFile> Files, List<TranspileDiagnostic> Diagnostics)
        Populate(IReadOnlyList<TgmlSourceFile> sources)
    {
        var files = new List<TgmlFile>();
        var diagnostics = new List<TranspileDiagnostic>();

        foreach (var source in sources)
        {
            try
            {
                var (file, fileDiags) = ParseFile(source);
                file.FileName.GetType(); // just to ensure non-null
                // Assign filename
                var namedFile = new TgmlFile
                {
                    FileName = source.FileName,
                    Usings = file.Usings,
                    Namespaces = file.Namespaces,
                    TypeDecls = file.TypeDecls
                };
                // Set QualifiedName on all top-level types from this file's namespace
                var ns = namedFile.PrimaryNamespace;
                foreach (var t in namedFile.TypeDecls)
                {
                    if (t.QualifiedName is null)
                    {
                        t.QualifiedName = string.IsNullOrEmpty(ns) ? t.Name : $"{ns}.{t.Name}";
                    }
                }

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

    private static (TgmlFile File, List<TranspileDiagnostic> Diagnostics)
        ParseFile(TgmlSourceFile source)
    {
        var diagnostics = new List<TranspileDiagnostic>();
        var lexerListener = new LexerErrorListener(source.FileName, diagnostics);
        var parserListener = new ParserErrorListener(source.FileName, diagnostics);

        var inputStream = new AntlrInputStream(source.Content);
        var lexer = new TypedGMLLexer(inputStream);
        lexer.RemoveErrorListeners();
        lexer.AddErrorListener(lexerListener);

        var tokenStream = new CommonTokenStream(lexer);
        var parser = new TypedGMLParser(tokenStream);
        parser.RemoveErrorListeners();
        parser.AddErrorListener(parserListener);

        var tree = parser.program();
        var visitor = new AstVisitor();
        var file = (TgmlFile)visitor.Visit(tree)!;

        return (file, diagnostics);
    }

    // ── ANTLR error listeners ────────────────────────────────────────────────

    /// <summary>Parser error listener (offending symbol is IToken).</summary>
    private sealed class ParserErrorListener
        : BaseErrorListener
    {
        private readonly List<TranspileDiagnostic> _diagnostics;
        private readonly string _fileName;

        public ParserErrorListener(string fileName, List<TranspileDiagnostic> diagnostics)
        {
            _fileName = fileName;
            _diagnostics = diagnostics;
        }

        public override void SyntaxError(
            TextWriter output,
            IRecognizer recognizer,
            IToken? offendingSymbol,
            int line,
            int charPositionInLine,
            string msg,
            RecognitionException e)
        {
            _diagnostics.Add(new TranspileDiagnostic(
                DiagnosticSeverity.Error,
                $"Syntax error: {msg}",
                _fileName, line, charPositionInLine));
        }
    }

    /// <summary>Lexer error listener (offending symbol is int token type).</summary>
    private sealed class LexerErrorListener
        : IAntlrErrorListener<int>
    {
        private readonly List<TranspileDiagnostic> _diagnostics;
        private readonly string _fileName;

        public LexerErrorListener(string fileName, List<TranspileDiagnostic> diagnostics)
        {
            _fileName = fileName;
            _diagnostics = diagnostics;
        }

        public void SyntaxError(
            TextWriter output,
            IRecognizer recognizer,
            int offendingSymbol,
            int line,
            int charPositionInLine,
            string msg,
            RecognitionException e)
        {
            _diagnostics.Add(new TranspileDiagnostic(
                DiagnosticSeverity.Error,
                $"Lexer error: {msg}",
                _fileName, line, charPositionInLine));
        }
    }
}