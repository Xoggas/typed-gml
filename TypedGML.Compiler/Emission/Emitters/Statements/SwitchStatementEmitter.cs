using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Statements;

namespace TypedGML.Compiler.Emission.Emitters.Statements;

public sealed class SwitchStatementEmitter : INodeEmitter
{
    public bool Matches(IAstNode node) => node is SwitchStatementNode;

    public void Emit(IAstNode node, EmitContext ctx)
    {
        var statement = (SwitchStatementNode)node;
        ctx.Writer.Write($"switch ({ctx.Emitter.Render(statement.Value, ctx)})");
        ctx.Writer.BeginBlock();
        foreach (var section in statement.Sections)
            EmitSection(section, ctx);
        ctx.Writer.EndBlock();
    }

    private static void EmitSection(SwitchSectionNode section, EmitContext ctx)
    {
        var label = section.Label is null ? "default:" : $"case {ctx.Emitter.Render(section.Label, ctx)}:";
        ctx.Writer.WriteLine(label);
        ctx.Writer.Indent();
        StatementEmitterHelper.EmitStatements(section.Statements, ctx);
        ctx.Writer.Dedent();
    }
}
