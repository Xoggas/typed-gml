using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Bcl;
using TypedGML.Compiler.Diagnostics;
using TypedGML.Compiler.Emission;
using TypedGML.Compiler.Parsing;
using TypedGML.Compiler.Population;
using TypedGML.Compiler.Symbols;
using TypedGML.Compiler.Verification;

namespace TypedGML.CLI;

internal sealed class CliCompiler(string bclPath)
{
    public BuildResult Compile(TgmlConfig config)
    {
        var diagnostics = new DiagnosticBag();
        var fileNodes = BuildAst(config, diagnostics);
        if (diagnostics.HasErrors)
            return new BuildResult(diagnostics, null);

        var symbols = Populate(fileNodes, diagnostics);
        if (diagnostics.HasErrors)
            return new BuildResult(diagnostics, null);

        Verify(fileNodes, symbols, diagnostics);
        if (diagnostics.HasErrors)
            return new BuildResult(diagnostics, null);

        var outputRoot = Path.Combine(Path.GetTempPath(), "TypedGML.CLI", Guid.NewGuid().ToString("N"));
        var outputSink = Emit(fileNodes, symbols, diagnostics, outputRoot);
        if (diagnostics.HasErrors)
            return new BuildResult(diagnostics, null);

        var metadata = CompileMetadataBuilder.Build(fileNodes, symbols);
        var bclScriptNames = BclScriptNameCollector.Collect(fileNodes, bclPath);
        return new BuildResult(diagnostics, CliCompileResult.FromOutput(outputSink.Output, outputRoot, metadata, bclScriptNames));
    }

    private IReadOnlyList<FileNode> BuildAst(TgmlConfig config, DiagnosticBag diagnostics)
    {
        var bclFiles = new BclLoader(bclPath).GetFiles();
        var userFiles = new SourceFileCollector().Collect(config);
        var astBuilder = new AstBuilder(diagnostics);
        return bclFiles.Concat(userFiles).Select(astBuilder.Build).ToList();
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
        var verifier = new Verifier(CliCompilerRegistrations.Checks(), diagnostics);
        verifier.Verify(files, symbols);
    }

    private static CollectingGmlOutputSink Emit(
        IReadOnlyList<FileNode> files,
        SymbolTable symbols,
        DiagnosticBag diagnostics,
        string outputRoot)
    {
        var outputSink = new CollectingGmlOutputSink(writeFiles: false);
        var emitter = new Emitter(
            CliCompilerRegistrations.Emitters(),
            new DecoratorProcessor(),
            new FileOrganizer(outputRoot),
            symbols,
            diagnostics,
            outputSink);
        emitter.Emit(files);
        return outputSink;
    }
}
