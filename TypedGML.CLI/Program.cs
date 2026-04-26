using TypedGML.Compiler;
using TypedGML.Compiler.Bcl;
using TypedGML.Compiler.Diagnostics;
using TypedGML.Compiler.Emission;
using TypedGML.Compiler.Emission.Emitters;
using TypedGML.Compiler.Emission.Emitters.Expressions;
using TypedGML.Compiler.Emission.Emitters.Statements;
using TypedGML.Compiler.Parsing;
using TypedGML.Compiler.Population;
using TypedGML.Compiler.Symbols;
using TypedGML.Compiler.Utils;
using TypedGML.Compiler.Verification;
using TypedGML.Compiler.Verification.Checks;

try
{
    var options = CompilerOptions.Parse(args);
    var diagnostics = new DiagnosticBag();

    var bclFiles = new BclLoader(options.BclPath).GetFiles();
    var userFiles = new FileScanner().Scan(options.InputPath);
    var allFiles = bclFiles.Concat(userFiles).ToList();

    var astBuilder = new AstBuilder(diagnostics);
    var fileNodes = allFiles.Select(f => astBuilder.Build(f)).ToList();
    if (diagnostics.HasErrors) return PrintAndExit(diagnostics);

    var symbolTable = new SymbolTable(diagnostics);
    var populator = new Populator(
        new NamespacePopulator(symbolTable, diagnostics),
        new TypePopulator(symbolTable, diagnostics),
        new MemberPopulator(symbolTable, diagnostics),
        new InheritanceResolver(symbolTable, diagnostics),
        new GenericParameterBinder(symbolTable, diagnostics));
    populator.Populate(fileNodes);
    if (diagnostics.HasErrors) return PrintAndExit(diagnostics);

    var checks = new ISemanticCheck[]
    {
        new TypeAssignabilityCheck(),
        new NullabilityCheck(),
        new MemberAccessCheck(),
        new MethodCallCheck(),
        new ConstructorCallCheck(),
        new OperatorCheck(),
        new ControlFlowCheck(),
        new AbstractCompletenessCheck(),
        new InterfaceImplementationCheck(),
        new OverrideSignatureCheck(),
        new SealedInheritanceCheck(),
        new StructInheritanceCheck(),
        new GenericConstraintCheck(),
        new DelegateSignatureCheck(),
        new EventAccessCheck(),
        new LambdaCheck(),
        new DecoratorPlacementCheck(),
        new ObjectDecoratorCheck(),
        new ConstExpressionCheck(),
        new ReadonlyAssignmentCheck(),
        new StaticModifierCheck(),
        new GlobalModifierCheck(),
        new WithTargetCheck(),
        new SwitchCaseConstantCheck(),
        new DuplicateCaseCheck(),
        new VarInferenceCheck(),
        new ArrayLiteralTypeCheck(),
        new ThrowTypeCheck(),
        new TypeofNameCheck(),
        new IsAsCompatibilityCheck(),
        new DuplicateMemberCheck(),
        new DuplicateParameterCheck(),
        new DefaultParameterConstCheck(),
        new ObjectCreationCheck(),
    };

    var verifier = new Verifier(checks, diagnostics);
    verifier.Verify(fileNodes, symbolTable);
    if (diagnostics.HasErrors) return PrintAndExit(diagnostics);

    var fileOrganizer = new FileOrganizer(options.OutputPath);
    var nodeEmitters = new INodeEmitter[]
    {
        new ClassEmitter(), new StructEmitter(), new EnumEmitter(), new DelegateEmitter(),
        new ConstructorEmitter(), new MethodEmitter(), new PropertyEmitter(),
        new IndexerEmitter(), new OperatorEmitter(), new FieldEmitter(), new EventEmitter(),
        new BlockStatementEmitter(), new IfStatementEmitter(), new WhileStatementEmitter(),
        new ForStatementEmitter(), new RepeatStatementEmitter(), new SwitchStatementEmitter(),
        new WithStatementEmitter(), new TryStatementEmitter(), new ReturnStatementEmitter(),
        new ThrowStatementEmitter(), new VarDeclarationStatementEmitter(),
        new ExpressionStatementEmitter(),
        new BinaryExpressionEmitter(), new UnaryExpressionEmitter(), new TernaryExpressionEmitter(),
        new AssignmentExpressionEmitter(), new MemberAccessExpressionEmitter(),
        new InvocationExpressionEmitter(), new ObjectCreationExpressionEmitter(),
        new LambdaExpressionEmitter(), new NullCoalescingExpressionEmitter(),
        new NullConditionalExpressionEmitter(), new ArrayLiteralExpressionEmitter(),
        new DictionaryLiteralExpressionEmitter(), new TypeofExpressionEmitter(),
        new NameofExpressionEmitter(), new DefaultExpressionEmitter(),
    };

    var emitter = new Emitter(nodeEmitters, new DecoratorProcessor(), fileOrganizer, symbolTable, diagnostics);
    emitter.Emit(fileNodes);

    PrintDiagnostics(diagnostics);
    return diagnostics.HasErrors ? 1 : 0;
}
catch (ArgumentException ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}
catch (DirectoryNotFoundException ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}

static int PrintAndExit(DiagnosticBag diagnostics)
{
    PrintDiagnostics(diagnostics);
    return 1;
}

static void PrintDiagnostics(DiagnosticBag diagnostics)
{
    foreach (var diagnostic in diagnostics.All)
    {
        var severity = diagnostic.Severity.ToString().ToLowerInvariant();
        var code = $"TGML{(int)diagnostic.Code:0000}";
        Console.Error.WriteLine($"{diagnostic.Location.FilePath}({diagnostic.Location.Line},{diagnostic.Location.Column}): {severity} {code}: {diagnostic.Message}");
    }
}
