using TypedGML.Compiler.Ast;

namespace TypedGML.Compiler.Emission;

internal static class EmitContextTempExtensions
{
    public static void ResetTempVars(this EmitContext ctx)
    {
        ctx.TempVariables.Counter = 0;
        ctx.TempVariables.Prelude.Clear();
    }

    public static string NextTempVarName(this EmitContext ctx) =>
        $"__tmp_{ctx.TempVarCounter++}";

    public static void AddTempPreludeLine(this EmitContext ctx, string line) =>
        ctx.TempVariables.Prelude.Add(line);

    public static void FlushTempPrelude(this EmitContext ctx)
    {
        foreach (var line in ctx.TempVariables.Prelude)
            ctx.Writer.WriteLine(line);
        ctx.TempVariables.Prelude.Clear();
    }

    public static string RenderWithTempPrelude(this EmitContext ctx, IAstNode? node)
    {
        ctx.TempVariables.PreludeDepth++;
        try
        {
            return ctx.Emitter.Render(node, ctx);
        }
        finally
        {
            ctx.TempVariables.PreludeDepth--;
        }
    }

    public static string RenderWithExpectedTempPrelude(this EmitContext ctx, IAstNode node, string? expectedType)
    {
        ctx.PushExpectedType(expectedType);
        try
        {
            return ctx.RenderWithTempPrelude(node);
        }
        finally
        {
            ctx.PopExpectedType();
        }
    }

    public static string RenderWithoutTempPrelude(this EmitContext ctx, IAstNode? node) =>
        ctx.RunWithoutTempPrelude(() => ctx.Emitter.Render(node, ctx));

    public static string RunWithoutTempPrelude(this EmitContext ctx, Func<string> render)
    {
        ctx.TempVariables.SuppressDepth++;
        try
        {
            return render();
        }
        finally
        {
            ctx.TempVariables.SuppressDepth--;
        }
    }
}
