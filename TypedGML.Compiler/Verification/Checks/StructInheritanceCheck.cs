using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Declarations;
using TypedGML.Compiler.Diagnostics;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Verification.Checks;

public sealed class StructInheritanceCheck : ISemanticCheck
{
    public bool Matches(IAstNode node) => node is StructDeclarationNode;

    public void Check(IAstNode node, VerificationContext ctx)
    {
        var @struct = (StructDeclarationNode)node;
        foreach (var baseType in @struct.BaseTypes)
            if (SymbolResolver.TryResolveType(baseType, ctx, out var resolved) && resolved.Kind != TypeKind.Interface)
                ctx.Diagnostics.Report(
                    DiagnosticCode.InvalidStructInheritance,
                    DiagnosticSeverity.Error,
                    $"Struct '{@struct.Name}' cannot inherit from non-interface type '{baseType}'.",
                    @struct.Location);
    }
}
