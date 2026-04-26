using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Declarations;
using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Verification;

internal static class DecoratorHelper
{
    public static IReadOnlyList<DecoratorNode> Decorators(IAstNode node) => node switch
    {
        ClassDeclarationNode n => n.Decorators,
        StructDeclarationNode n => n.Decorators,
        InterfaceDeclarationNode n => n.Decorators,
        EnumDeclarationNode n => n.Decorators,
        EnumMemberNode n => n.Decorators,
        DelegateDeclarationNode n => n.Decorators,
        FunctionDeclarationNode n => n.Decorators,
        FieldDeclarationNode n => n.Decorators,
        PropertyDeclarationNode n => n.Decorators,
        IndexerDeclarationNode n => n.Decorators,
        MethodDeclarationNode n => n.Decorators,
        ConstructorDeclarationNode n => n.Decorators,
        OperatorDeclarationNode n => n.Decorators,
        ConversionOperatorNode n => n.Decorators,
        EventDeclarationNode n => n.Decorators,
        ParameterNode n => n.Decorators,
        _ => []
    };

    public static DecoratorNode? Find(IAstNode node, string name) =>
        Decorators(node).FirstOrDefault(decorator => decorator.Name == name);

    public static bool IsBcl(SourceLocation location)
    {
        var path = location.FilePath.Replace('\\', '/');
        return path.Contains("/bcl/", StringComparison.OrdinalIgnoreCase) ||
               path.EndsWith("/bcl", StringComparison.OrdinalIgnoreCase);
    }
}
