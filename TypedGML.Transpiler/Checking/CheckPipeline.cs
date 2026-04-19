using TypedGML.Transpiler.Checking.Checks;
using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Checking;

/// <summary>
///     An ordered sequence of <see cref="CheckBatch" />es.
///     Each batch runs to completion; if errors accumulated the pipeline stops before the next batch.
/// </summary>
public sealed class CheckPipeline
{
    private readonly List<CheckBatch> _batches = new();

    /// <summary>The default pipeline used by <see cref="TranspilerApi" />.</summary>
    public static CheckPipeline Default { get; } = BuildDefault();

    public CheckPipeline AddBatch(CheckBatch batch)
    {
        _batches.Add(batch);
        return this;
    }

    public void Run(TranspileContext context, IReadOnlyList<TgmlFile> files)
    {
        foreach (var batch in _batches)
        {
            foreach (var check in batch.Checks)
                check.Execute(context, files);

            if (context.HasErrors) return;
        }
    }

    private static CheckPipeline BuildDefault()
    {
        var pipeline = new CheckPipeline();

        // ── Batch 1: Type Registration ────────────────────────────────────────
        // Registers all types in the TypeTable, detects duplicate declarations and
        // obvious structural mistakes (constructor mis-naming, duplicate members).
        pipeline.AddBatch(new CheckBatch { Name = "TypeRegistration" }
            .Add(new TypeRegistrationCheck())
            .Add(new DuplicateTypeCheck())
            .Add(new DuplicateMemberCheck())
            .Add(new OperatorDeclarationCheck())
            .Add(new AssetDecoratorCheck())
            .Add(new AccessorModifierCheck())
            .Add(new IndexerDeclarationCheck())
            .Add(new NativePropertyBehaviorCheck())
            .Add(new ConstructorNameCheck())
            .Add(new DecoratorArgConstantCheck()));

        // ── Batch 2: Type Hierarchy ───────────────────────────────────────────
        // Validates the inheritance graph.  Circular detection runs first so
        // subsequent checks can safely traverse the chain.
        pipeline.AddBatch(new CheckBatch { Name = "TypeHierarchy" }
            .Add(new CircularInheritanceCheck())
            .Add(new InheritanceCheck())
            .Add(new OverrideCorrectnessCheck())
            .Add(new InterfaceImplementationCheck()));

        // ── Batch 3: Generic Validation ───────────────────────────────────────
        pipeline.AddBatch(new CheckBatch { Name = "GenericValidation" }
            .Add(new TypeReferenceCheck())
            .Add(new GenericArityCheck())
            .Add(new GenericConstraintCheck()));

        // ── Batch 4: Body Semantics ───────────────────────────────────────────
        // Checks that operate on statement / expression bodies.
        pipeline.AddBatch(new CheckBatch { Name = "BodySemantics" }
            .Add(new DefaultParameterValueCheck())
            .Add(new FieldKeywordContextCheck())
            .Add(new OperandTypeCheck())       // operator types, assignment, returns
            .Add(new AbstractInstantiationCheck())
            .Add(new ConstMutationCheck())
            .Add(new BreakContinueContextCheck())
            .Add(new UnreachableCodeCheck()));

        return pipeline;
    }
}
