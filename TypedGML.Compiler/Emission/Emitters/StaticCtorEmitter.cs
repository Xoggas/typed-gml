using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Ast.Statements;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Emission.Emitters;

public sealed class StaticCtorEmitter
{
    private readonly StaticAutoPropertyInitializerEmitter _autoPropertyInitializerEmitter = new();
    public void EmitStaticCtor(TypeSymbol type, IEnumerable<IAstNode> members, EmitContext ctx)
    {
        var staticMethods = members.OfType<MethodDeclarationNode>().Where(IsStatic).ToList();
        var staticFields = members.OfType<FieldDeclarationNode>().Where(IsStatic).ToList();
        var staticProperties = members.OfType<PropertyDeclarationNode>().Where(IsStatic).ToList();
        var staticConstructor = members.OfType<StaticConstructorDeclarationNode>().FirstOrDefault();
        var assignedStaticFields = StaticCtorAssignmentFinder.FindAssignedFields(type, staticConstructor);
        if (staticMethods.Count == 0 && staticFields.Count == 0 && staticProperties.Count == 0 && staticConstructor is null)
            return;

        ctx.Writer.Write($"function {NamingConvention.StaticCtorFunctionName(type)}()");
        ctx.ResetTempVars();
        ctx.Writer.BeginBlock();
        staticMethods.ForEach(method => EmitMethod(type, method, ctx));
        staticFields.Where(field => !assignedStaticFields.Contains(field.Name)).ToList().ForEach(field => EmitField(type, field, ctx));
        _autoPropertyInitializerEmitter.Emit(type, staticProperties, ctx);
        staticProperties.ForEach(property => EmitProperty(type, property, ctx));
        if (staticConstructor is not null)
            EmitBody(staticConstructor.Body, ctx);
        ctx.Writer.EndBlock();
        ctx.Writer.WriteLine($"gml_pragma(\"global\", \"{NamingConvention.StaticCtorFunctionName(type)}()\");");
    }

    private static void EmitMethod(TypeSymbol type, MethodDeclarationNode method, EmitContext ctx)
    {
        var symbol = ResolveMethod(type, method);
        if (symbol is null)
            return;

        var parameters = string.Join(", ", method.Parameters.Select(p => p.Name));
        ctx.Writer.Write($"{NamingConvention.StaticMemberName(type, symbol)} = function({parameters})");
        ctx.ResetTempVars();
        ctx.Writer.BeginBlock();
        var nativeCall = DecoratorArg(method.Decorators, "NativeCall");
        if (nativeCall is not null)
            EmitNativeCall(method, nativeCall, ctx);
        else if (method.Body is not null)
            EmitBody(method.Body, ctx);
        ctx.Writer.Dedent();
        ctx.Writer.WriteLine("};");
    }

    private static void EmitField(TypeSymbol type, FieldDeclarationNode field, EmitContext ctx)
    {
        var symbol = type.Members.FirstOrDefault(m => m.Kind == MemberKind.Field && m.Name == field.Name);
        if (symbol is null)
            return;
        var value = field.Initializer is null ? DefaultValueRenderer.Render(new DefaultExpressionNode(field.TypeRef, field.Location), ctx) : ctx.RenderWithExpectedTempPrelude(field.Initializer, field.TypeRef);
        ctx.FlushTempPrelude();
        ctx.Writer.WriteLine($"{NamingConvention.StaticMemberName(type, symbol)} = {value};");
    }

    private static void EmitProperty(TypeSymbol type, PropertyDeclarationNode property, EmitContext ctx)
    {
        var symbol = type.Members.FirstOrDefault(m => m.Kind == MemberKind.Property && m.Name == property.Name);
        if (symbol is null)
            return;

        foreach (var accessor in property.Accessors)
            EmitAccessor(type, property, symbol, accessor, ctx);
    }

    private static void EmitAccessor(TypeSymbol type, PropertyDeclarationNode property, MemberSymbol symbol, AccessorNode accessor, EmitContext ctx)
    {
        if (accessor.Kind == AccessorKind.Get)
        {
            ctx.Writer.Write($"{NamingConvention.StaticGetterName(type, symbol)} = function()");
            ctx.ResetTempVars();
            ctx.Writer.BeginBlock();
            if (accessor.Body is null)
                ctx.Writer.WriteLine($"return {PropertyTarget(type, property, symbol)};");
            else
                EmitBody(accessor.Body, ctx);
            ctx.Writer.Dedent();
            ctx.Writer.WriteLine("};");
            return;
        }

        ctx.Writer.Write($"{NamingConvention.StaticSetterName(type, symbol)} = function(value)");
        ctx.ResetTempVars();
        ctx.Writer.BeginBlock();
        if (accessor.Body is null)
            ctx.Writer.WriteLine($"{PropertyTarget(type, property, symbol)} = value;");
        else
            EmitBody(accessor.Body, ctx);
        ctx.Writer.Dedent();
        ctx.Writer.WriteLine("};");
    }

    private static string PropertyTarget(TypeSymbol type, PropertyDeclarationNode property, MemberSymbol symbol) =>
        DecoratorArg(property.Decorators, "NativeProperty") ??
        DecoratorArg(property.Decorators, "Asset") ??
        NamingConvention.StaticPropertyBackingName(type, symbol);

    private static void EmitBody(IAstNode body, EmitContext ctx)
    {
        if (body is BlockStatementNode block)
        {
            foreach (var statement in block.Statements)
                ctx.Emitter.Emit(statement, ctx);
            return;
        }

        ctx.Emitter.Emit(body, ctx);
    }

    private static void EmitNativeCall(MethodDeclarationNode method, string nativeCall, EmitContext ctx)
    {
        var argNames = method.Parameters.Select(p => p.Name).ToArray();
        if (IntrinsicOpEmitter.TryEmit(nativeCall, argNames, method.TypeRef, ctx.Writer))
            return;

        var invocation = $"{nativeCall}({string.Join(", ", argNames)})";
        if (string.Equals(method.TypeRef, "void", StringComparison.Ordinal))
            ctx.Writer.WriteLine($"{invocation};");
        else
            ctx.Writer.WriteLine($"return {invocation};");
    }

    private static MemberSymbol? ResolveMethod(TypeSymbol type, MethodDeclarationNode method) =>
        type.Members.FirstOrDefault(member =>
            member.Kind == MemberKind.Method &&
            member.Name == method.Name &&
            member.Parameters.Select(p => p.TypeRef).SequenceEqual(method.Parameters.Select(p => p.TypeRef), StringComparer.Ordinal));

    private static bool IsStatic(FieldDeclarationNode field) =>
        field.Modifiers.Contains("static", StringComparer.Ordinal);

    private static bool IsStatic(MethodDeclarationNode method) =>
        method.Modifiers.Contains("static", StringComparer.Ordinal);

    private static bool IsStatic(PropertyDeclarationNode property) =>
        property.Modifiers.Contains("static", StringComparer.Ordinal);

    private static string? DecoratorArg(IReadOnlyList<DecoratorNode> decorators, string name) =>
        decorators.FirstOrDefault(d => d.Name == name)?.Args.FirstOrDefault() is LiteralExpressionNode literal
            ? literal.Value?.ToString()
            : null;
}
