using TypedGML.Transpiler.Generation.Decorators;
using TypedGML.Transpiler.Generation.Emitters;
using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Generation;

/// <summary>
///     Orchestrates the generation stage:
///     1. Applies decorator handlers to all type declarations.
///     2. Routes each type to the appropriate emitter.
///     3. Adds the __TypeMacros file.
/// </summary>
public sealed class Generator
{
    private readonly List<ICodeEmitter> _emitters;
    private readonly TypeMacroEmitter _macroEmitter;
    private readonly DecoratorRegistry _registry;

    public Generator(DecoratorRegistry? registry = null)
    {
        _registry = registry ?? DecoratorRegistry.Default;
        _macroEmitter = new TypeMacroEmitter();
        _emitters = new List<ICodeEmitter>
        {
            new GameObjectEmitter(),
            new DelegateEmitter(),
            new ScriptClassEmitter(),
            new EnumEmitter()
            // Interfaces produce no GML code (type-check only)
        };
    }

    public List<GeneratedFile> Generate(TranspileContext ctx, IReadOnlyList<TgmlFile> files)
    {
        var genCtx = new GenerationContext
        {
            TranspileContext = ctx,
            DecoratorRegistry = _registry
        };

        var output = new List<GeneratedFile>();

        // Pass 1: apply all decorators
        foreach (var file in files)
        foreach (var type in file.TypeDecls)
        {
            _registry.ApplyAll(type, ctx);
        }

        // Pass 2: emit each type
        foreach (var file in files)
        foreach (var type in file.TypeDecls)
        {
            genCtx.CurrentType = type;
            var emitter = _emitters.FirstOrDefault(e => e.CanEmit(type));
            if (emitter is not null)
            {
                output.AddRange(emitter.Emit(type, genCtx));
            }
        }

        // Pass 3: type macros
        output.Add(_macroEmitter.Emit(ctx));

        return output;
    }
}
