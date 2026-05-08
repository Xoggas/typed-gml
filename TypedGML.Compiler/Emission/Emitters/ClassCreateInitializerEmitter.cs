using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Declarations;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Emission.Emitters;

internal sealed class ClassCreateInitializerEmitter
{
    public bool HasInitializers(ClassDeclarationNode declaration, EmitContext ctx) =>
        Declarations(declaration, ctx).Any(frame =>
            InstanceFields(frame.Declaration.Members).Any() ||
            InstanceAutoProperties(frame.Declaration.Members).Any() ||
            InstanceEvents(frame.Declaration.Members).Any());

    public void Emit(ClassDeclarationNode declaration, EmitContext ctx)
    {
        foreach (var frame in Declarations(declaration, ctx))
            WithCurrentType(ctx, frame.Type, () =>
            {
                EmitFields(frame.Declaration, ctx);
                EmitAutoProperties(frame.Declaration, ctx);
                EmitEvents(frame.Declaration, frame.Type, ctx);
            });
    }

    private static void EmitFields(ClassDeclarationNode declaration, EmitContext ctx)
    {
        foreach (var field in InstanceFields(declaration.Members))
        {
            var value = field.Initializer is null
                ? DefaultValueRenderer.Render(new DefaultExpressionNode(field.TypeRef, field.Location), ctx)
                : ctx.RenderWithExpectedTempPrelude(field.Initializer, field.TypeRef);
            ctx.FlushTempPrelude();
            ctx.Writer.WriteLine($"self.{field.Name} = {value};");
        }
    }

    private static void EmitAutoProperties(ClassDeclarationNode declaration, EmitContext ctx)
    {
        foreach (var property in InstanceAutoProperties(declaration.Members))
        {
            var target = NamingConvention.InstancePropertyBackingName("self", property.Name);
            var value = AutoPropertyEmitterHelper.RenderInitialValue(property, ctx);
            ctx.FlushTempPrelude();
            ctx.Writer.WriteLine($"{target} = {value};");
        }
    }

    private static void EmitEvents(ClassDeclarationNode declaration, TypeSymbol type, EmitContext ctx)
    {
        foreach (var evt in InstanceEvents(declaration.Members))
        {
            var symbol = type.Members.FirstOrDefault(member => member.Kind == MemberKind.Event && member.Name == evt.Name);
            if (symbol is not null)
                ctx.Writer.WriteLine($"{NamingConvention.InstanceEventBackingName("self", symbol)} = [];");
        }
    }

    private static IReadOnlyList<(TypeSymbol Type, ClassDeclarationNode Declaration)> Declarations(
        ClassDeclarationNode declaration,
        EmitContext ctx)
    {
        var frames = new List<(TypeSymbol Type, ClassDeclarationNode Declaration)>();
        for (var current = ctx.CurrentType; current is not null; current = current.Base)
        {
            if (ctx.TypeDeclarations.TryGetValue(TypeDeclarationMapBuilder.Key(current), out var node) &&
                node is ClassDeclarationNode classDeclaration)
                frames.Add((current, classDeclaration));
        }

        if (frames.Count == 0 && ctx.CurrentType is not null)
            frames.Add((ctx.CurrentType, declaration));

        frames.Reverse();
        return frames;
    }

    private static IEnumerable<FieldDeclarationNode> InstanceFields(IEnumerable<IAstNode> members) =>
        members.OfType<FieldDeclarationNode>().Where(field =>
            !field.Modifiers.Contains("static", StringComparer.Ordinal) &&
            !field.Modifiers.Contains("const", StringComparer.Ordinal));

    private static IEnumerable<PropertyDeclarationNode> InstanceAutoProperties(IEnumerable<IAstNode> members) =>
        members.OfType<PropertyDeclarationNode>().Where(IsInstanceAutoProperty);

    private static bool IsInstanceAutoProperty(PropertyDeclarationNode property) =>
        !property.Modifiers.Contains("static", StringComparer.Ordinal) &&
        !property.Modifiers.Contains("abstract", StringComparer.Ordinal) &&
        AutoPropertyEmitterHelper.IsCompilerBacked(property);

    private static IEnumerable<EventDeclarationNode> InstanceEvents(IEnumerable<IAstNode> members) =>
        members.OfType<EventDeclarationNode>().Where(evt => !evt.Modifiers.Contains("static", StringComparer.Ordinal));

    private static void WithCurrentType(EmitContext ctx, TypeSymbol type, Action action)
    {
        var previousType = ctx.CurrentType;
        ctx.CurrentType = type;
        try
        {
            action();
        }
        finally
        {
            ctx.CurrentType = previousType;
        }
    }
}
