using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Members;

namespace TypedGML.Compiler.Emission.Emitters;

public sealed class IndexerEmitter : INodeEmitter
{
    public bool Matches(IAstNode node) => node is IndexerDeclarationNode;

    public void Emit(IAstNode node, EmitContext ctx)
    {
        var indexer = (IndexerDeclarationNode)node;
        if (ctx.CurrentType is null)
            return;

        foreach (var accessor in indexer.Accessors)
        {
            var name = accessor.Kind == AccessorKind.Get ? "get" : "set";
            var parameters = accessor.Kind == AccessorKind.Get ? indexer.Parameter.Name : $"{indexer.Parameter.Name}, value";
            ctx.Writer.Write($"function {NamingConvention.TypeName(ctx.CurrentType)}_{name}_indexer({parameters})");
            ctx.Writer.BeginBlock();
            if (accessor.Kind == AccessorKind.Get)
                ctx.Writer.WriteLine($"return self[{indexer.Parameter.Name}];");
            else
                ctx.Writer.WriteLine($"self[{indexer.Parameter.Name}] = value;");
            ctx.Writer.EndBlock();
        }
    }
}
