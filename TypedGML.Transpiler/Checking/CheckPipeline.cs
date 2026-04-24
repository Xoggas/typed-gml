using TypedGML.Transpiler.Checking.Checks;
using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking;

/// <summary>
///     An ordered sequence of checks grouped into phases.
///     Each phase runs to completion; if errors accumulated the pipeline stops before the next phase.
///     Phases exist purely for ordering correctness (e.g. types must be registered before hierarchy
///     can be validated) — they are not a public API concept.
/// </summary>
public sealed class CheckPipeline
{
    private readonly List<List<IAtomicCheck>> _phases = new();

    /// <summary>The default pipeline used by <see cref="TranspilerApi" />.</summary>
    public static CheckPipeline Default { get; } = BuildDefault();

    /// <summary>Appends a new phase containing the given checks.</summary>
    public CheckPipeline AddPhase(params IAtomicCheck[] checks)
    {
        _phases.Add([.. checks]);
        return this;
    }

    /// <summary>
    ///     Executes all phases against <paramref name="files"/>.
    ///     Stops after any phase that produces errors so that later phases do not
    ///     operate on an inconsistent type graph.
    /// </summary>
    /// <param name="context">The shared transpile context that accumulates diagnostics.</param>
    /// <param name="files">The parsed source files to check.</param>
    public void Run(TranspileContext context, IReadOnlyList<TgmlFile> files)
    {
        foreach (var phase in _phases)
        {
            foreach (var check in phase)
                check.Execute(context, files);

            if (context.HasErrors) return;
        }
    }

    private static CheckPipeline BuildDefault() => new CheckPipeline()
        // Phase 1: Type Registration
        // Registers all types in the TypeTable, detects duplicate declarations and
        // obvious structural mistakes (constructor mis-naming, duplicate members).
        .AddPhase(
            new TypeRegistrationCheck(),
            new DuplicateTypeCheck(),
            new DuplicateMemberCheck(),
            new ReservedMemberCheck(),
            new OperatorDeclarationCheck(),
            new AssetDecoratorCheck(),
            new AccessorModifierCheck(),
            new IndexerDeclarationCheck(),
            new NativePropertyBehaviorCheck(),
            new ConstructorNameCheck(),
            new DecoratorArgConstantCheck(),
            new ReadonlyStructCheck())
        // Phase 2: Type Hierarchy
        // Validates the inheritance graph. Circular detection runs first so
        // subsequent checks can safely traverse the chain.
        .AddPhase(
            new CircularInheritanceCheck(),
            new InheritanceCheck(),
            new OverrideCorrectnessCheck(),
            new InterfaceImplementationCheck())
        // Phase 3: Generic Validation
        .AddPhase(
            new TypeReferenceCheck(),
            new GenericArityCheck(),
            new GenericConstraintCheck())
        // Phase 4: Body Semantics
        // Checks that operate on statement / expression bodies.
        .AddPhase(
            new DefaultParameterValueCheck(),
            new FieldKeywordContextCheck(),
            new OperandTypeCheck(),
            new AbstractInstantiationCheck(),
            new ConstMutationCheck(),
            new BreakContinueContextCheck(),
            new UnreachableCodeCheck(),
            new GenericConstraintBodyCheck());
}
