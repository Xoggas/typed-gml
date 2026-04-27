using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Bcl;
using TypedGML.Compiler.Diagnostics;
using TypedGML.Compiler.Parsing;
using TypedGML.Compiler.Population;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Tests.Helpers;

internal sealed class PopulationPipelineRunner
{
    public CompileResult Compile(string tgmlSource)
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "TypedGML.Compiler.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        var sourcePath = Path.Combine(tempDirectory, "Test.tgml");
        File.WriteAllText(sourcePath, tgmlSource);

        try
        {
            return Populate(sourcePath);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    private static CompileResult Populate(string sourcePath)
    {
        var diagnostics = new DiagnosticBag();
        var files = BuildAst(sourcePath, diagnostics);
        if (diagnostics.HasErrors)
            return new CompileResult(diagnostics.Errors, diagnostics.Warnings, new Dictionary<string, string>());

        var symbols = new SymbolTable(diagnostics);
        var namespacePopulator = new NamespacePopulator(symbols, diagnostics);
        var populator = new Populator(
            namespacePopulator,
            new TypePopulator(symbols, diagnostics, namespacePopulator),
            new MemberPopulator(symbols, diagnostics),
            new InheritanceResolver(symbols, diagnostics),
            new GenericParameterBinder(symbols, diagnostics));
        populator.Populate(files);
        return new CompileResult(diagnostics.Errors, diagnostics.Warnings, new Dictionary<string, string>());
    }

    private static IReadOnlyList<FileNode> BuildAst(string sourcePath, DiagnosticBag diagnostics)
    {
        var bclFiles = new BclLoader(FindBclPath()).GetFiles();
        var allFiles = bclFiles.Concat([sourcePath]).ToList();
        var astBuilder = new AstBuilder(diagnostics);
        return allFiles.Select(astBuilder.Build).ToList();
    }

    private static string FindBclPath()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, "bcl");
            if (Directory.Exists(candidate))
                return candidate;
        }

        return Path.Combine(AppContext.BaseDirectory, "bcl");
    }
}
