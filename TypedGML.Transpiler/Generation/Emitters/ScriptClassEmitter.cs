using TypedGML.Transpiler.Checking;
using TypedGML.Transpiler.Generation.Emitters.Atomic;
using TypedGML.Transpiler.Generation.Helpers;
using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Generation.Emitters;

public sealed partial class ScriptClassEmitter : ICodeEmitter
{
    public bool CanEmit(TgmlTypeDecl decl)
        => (decl is TgmlClassDecl cls && !cls.IsGameObject) || decl is TgmlStructDecl;

    public IEnumerable<GeneratedFile> Emit(TgmlTypeDecl decl, GenerationContext ctx)
    {
        ctx.CurrentType = decl;

        var (fields, properties, methods, constructors, _, typeParams, name) = decl switch
        {
            TgmlClassDecl c => (c.Fields, c.Properties, c.Methods, c.Constructors, c.BaseTypes, c.TypeParams, c.Name),
            TgmlStructDecl s => (s.Fields, s.Properties, s.Methods, s.Constructors, s.BaseTypes, s.TypeParams, s.Name),
            _ => throw new InvalidOperationException($"ScriptClassEmitter cannot emit {decl.GetType().Name}.")
        };

        var gmlName = decl.QualifiedName?.Replace(".", "_") ?? name;
        var qualifiedTypeName = decl.QualifiedName ?? name;
        var stmtEmit = new StatementEmitter(ctx);
        var exprEmit = new ExpressionEmitter(ctx);
        var writer = new GmlWriter();

        if (OperatorHelperEmitter.Emit(decl, methods, ctx, writer))
            writer.WriteLine();

        var typeIds = EmitHelpers.BuildTypesStruct(decl, ctx);
        var typeArgParams = Enumerable.Range(0, typeParams.Count).Select(i => $"__t{i}").ToList();
        var typeArgOffset = typeArgParams.Count;

        if (constructors.Count <= 1)
            EmitSingleCtorBody(decl, gmlName, qualifiedTypeName, fields, properties, methods, constructors, typeIds, typeArgParams, ctx, exprEmit, stmtEmit, writer);
        else
            EmitMultiCtorBody(decl, gmlName, qualifiedTypeName, fields, properties, methods, constructors, typeIds, typeArgParams, typeArgOffset, ctx, exprEmit, stmtEmit, writer);

        yield return new GeneratedFile($"Scripts/{gmlName}/{gmlName}.gml", writer.ToString());
    }

    private static bool IsCompilerSynthesized(TgmlMethodDecl method) => method.Name is "GetType";
}
