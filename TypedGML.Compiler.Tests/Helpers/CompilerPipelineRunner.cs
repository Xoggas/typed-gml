using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Bcl;
using TypedGML.Compiler.Diagnostics;
using TypedGML.Compiler.Emission;
using TypedGML.Compiler.Parsing;
using TypedGML.Compiler.Population;
using TypedGML.Compiler.Symbols;
using TypedGML.Compiler.Verification;

namespace TypedGML.Compiler.Tests.Helpers;

internal sealed class CompilerPipelineRunner
{
    public CompileResult Compile(string tgmlSource)
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "TypedGML.Compiler.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        var sourcePath = Path.Combine(tempDirectory, "Test.tgml");
        File.WriteAllText(sourcePath, tgmlSource);

        try
        {
            return CompileFile(sourcePath, tempDirectory);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    private static CompileResult CompileFile(string sourcePath, string tempDirectory)
    {
        var diagnostics = new DiagnosticBag();
        var files = BuildAst(sourcePath, diagnostics);
        if (diagnostics.HasErrors)
            return Result(diagnostics, new InMemoryGmlOutputSink());

        var symbols = Populate(files, diagnostics);
        if (diagnostics.HasErrors)
            return Result(diagnostics, new InMemoryGmlOutputSink());

        Verify(files, symbols, diagnostics);
        if (diagnostics.HasErrors)
            return Result(diagnostics, new InMemoryGmlOutputSink());

        var output = Emit(files, symbols, diagnostics, tempDirectory);
        return Result(diagnostics, output);
    }

    private static IReadOnlyList<FileNode> BuildAst(string sourcePath, DiagnosticBag diagnostics)
    {
        var bclFiles = new BclLoader(FindBclPath()).GetFiles();
        var allFiles = bclFiles.Concat([sourcePath]).ToList();
        var astBuilder = new AstBuilder(diagnostics);
        return allFiles.Select(astBuilder.Build).ToList();
    }

    private static SymbolTable Populate(IReadOnlyList<FileNode> files, DiagnosticBag diagnostics)
    {
        var symbols = new SymbolTable(diagnostics);
        var namespacePopulator = new NamespacePopulator(symbols, diagnostics);
        var populator = new Populator(
            namespacePopulator,
            new TypePopulator(symbols, diagnostics, namespacePopulator),
            new MemberPopulator(symbols, diagnostics),
            new InheritanceResolver(symbols, diagnostics),
            new GenericParameterBinder(symbols, diagnostics));
        populator.Populate(files);
        return symbols;
    }

    private static void Verify(IReadOnlyList<FileNode> files, SymbolTable symbols, DiagnosticBag diagnostics)
    {
        var verifier = new Verifier(CompilerRegistrations.Checks(), diagnostics);
        verifier.Verify(files, symbols);
    }

    private static InMemoryGmlOutputSink Emit(
        IReadOnlyList<FileNode> files,
        SymbolTable symbols,
        DiagnosticBag diagnostics,
        string tempDirectory)
    {
        var output = new InMemoryGmlOutputSink();
        var fileOrganizer = new FileOrganizer(Path.Combine(tempDirectory, "out"));
        var emitter = new Emitter(CompilerRegistrations.Emitters(), new DecoratorProcessor(), fileOrganizer, symbols, diagnostics, output);
        emitter.Emit(files);
        return output;
    }

    private static CompileResult Result(DiagnosticBag diagnostics, InMemoryGmlOutputSink output) =>
        new(diagnostics.Errors, diagnostics.Warnings, output.Output);

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
