using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Declarations;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Ast.Statements;
using TypedGML.Compiler.Diagnostics;

namespace TypedGML.Compiler.Verification.Checks;

public sealed class TypeAssignabilityCheck : ISemanticCheck
{
    public bool Matches(IAstNode node) =>
        node is AssignmentExpressionNode or VarDeclarationStatementNode or ReturnStatementNode or MethodDeclarationNode or FunctionDeclarationNode or AccessorNode;

    public void Check(IAstNode node, VerificationContext ctx)
    {
        switch (node)
        {
            case AssignmentExpressionNode assignment:
                CheckAssignment(assignment, ctx);
                break;
            case VarDeclarationStatementNode declaration:
                CheckDeclaration(declaration, ctx);
                break;
            case ReturnStatementNode @return:
                CheckReturn(@return, ctx);
                break;
            case MethodDeclarationNode method:
                CheckBodyReturn(method.TypeRef, method.Body, method.Location, ctx);
                break;
            case FunctionDeclarationNode function:
                CheckBodyReturn(function.ReturnType, function.Body, function.Location, ctx);
                break;
            case AccessorNode accessor:
                CheckBodyReturn(ctx.CurrentMember?.ReturnType ?? string.Empty, accessor.Body, accessor.Location, ctx);
                break;
        }
    }

    private static void CheckAssignment(AssignmentExpressionNode assignment, VerificationContext ctx)
    {
        var targetType = ExpressionTypeResolver.Resolve(assignment.Target, ctx);
        if (assignment.Op is "+=" or "-=" && DelegateAssignmentHelper.IsDelegateOrEventAssignment(assignment.Target, targetType, ctx))
            return;

        if (assignment.Value is DictionaryLiteralExpressionNode)
        {
            if (TypeReferenceHelper.RootName(targetType) != "Dictionary")
            {
                Report(DiagnosticCode.InvalidDictionaryLiteralTarget, "Dictionary literals are only valid when the target type is Dictionary<K,V>.", assignment.Location, ctx);
                return;
            }

            return;
        }
        var valueType = ExpressionTypeResolver.Resolve(assignment.Value, ctx);
        if (IsEmptyArrayForArrayTarget(assignment.Value, targetType))
            return;

        if (!TypeReferenceHelper.IsAssignable(targetType, valueType, ctx))
            Report(DiagnosticCode.TypeMismatch, $"Cannot assign '{valueType ?? "unknown"}' to '{targetType ?? "unknown"}'.", assignment.Location, ctx);
    }

    private static void CheckDeclaration(VarDeclarationStatementNode declaration, VerificationContext ctx)
    {
        if (declaration.Initializer is null)
            return;

        var initializerType = ExpressionTypeResolver.Resolve(declaration.Initializer, ctx);
        if (declaration.IsVar && declaration.Initializer is LiteralExpressionNode { Kind: LiteralKind.Null })
            Report(DiagnosticCode.AmbiguousVarInference, $"Cannot infer the type of '{declaration.Name}' from null.", declaration.Location, ctx);

        if (declaration.IsVar && string.IsNullOrWhiteSpace(initializerType))
            Report(DiagnosticCode.AmbiguousVarInference, $"Cannot determine a unique type for '{declaration.Name}'.", declaration.Location, ctx);

        if (declaration.Initializer is DictionaryLiteralExpressionNode)
        {
            if (TypeReferenceHelper.RootName(declaration.TypeRef) != "Dictionary")
            {
                Report(DiagnosticCode.InvalidDictionaryLiteralTarget, "Dictionary literals are only valid when the target type is Dictionary<K,V>.", declaration.Location, ctx);
                return;
            }

            return;
        }

        if (!declaration.IsVar && !TypeReferenceHelper.IsAssignable(declaration.TypeRef, initializerType, ctx))
        {
            if (IsEmptyArrayForArrayTarget(declaration.Initializer, declaration.TypeRef))
                return;

            Report(DiagnosticCode.TypeMismatch, $"Cannot assign '{initializerType ?? "unknown"}' to '{declaration.TypeRef ?? "unknown"}'.", declaration.Location, ctx);
        }
    }

    private static void CheckReturn(ReturnStatementNode @return, VerificationContext ctx)
    {
        var expectedType = ctx.CurrentMember?.ReturnType;
        if (string.IsNullOrWhiteSpace(expectedType))
            return;

        if (expectedType == "void" && @return.Value is not null)
            Report(DiagnosticCode.InvalidReturnUsage, "Void member cannot return a value.", @return.Location, ctx);

        if (expectedType != "void" && @return.Value is null)
            Report(DiagnosticCode.MissingReturnInNonVoidMethod, $"Member returning '{expectedType}' must return a value.", @return.Location, ctx);

        if (expectedType != "void" && @return.Value is not null)
        {
            if (@return.Value is DictionaryLiteralExpressionNode &&
                TypeReferenceHelper.RootName(expectedType) == "Dictionary")
                return;

            var valueType = ExpressionTypeResolver.Resolve(@return.Value, ctx);
            if (IsEmptyArrayForArrayTarget(@return.Value, expectedType))
                return;

            if (!TypeReferenceHelper.IsAssignable(expectedType, valueType, ctx))
                Report(DiagnosticCode.TypeMismatch, $"Cannot return '{valueType ?? "unknown"}' from a member returning '{expectedType}'.", @return.Location, ctx);
        }
    }

    private static void CheckBodyReturn(string returnType, IAstNode? body, SourceLocation location, VerificationContext ctx)
    {
        if (string.IsNullOrWhiteSpace(returnType) || returnType == "void" || body is null)
            return;

        if (!ReturnsOnAllPaths(body))
            Report(DiagnosticCode.MissingReturnInNonVoidMethod, $"Not all code paths return a value of type '{returnType}'.", location, ctx);
    }

    private static bool ReturnsOnAllPaths(IAstNode node) => node switch
    {
        ReturnStatementNode => true,
        BlockStatementNode block => block.Statements.Any(ReturnsOnAllPaths),
        IfStatementNode @if => ReturnsOnAllPaths(@if.ThenBlock) &&
                               @if.ElseIfClauses.All(clause => ReturnsOnAllPaths(clause.ThenBlock)) &&
                               @if.ElseBlock is not null &&
                               ReturnsOnAllPaths(@if.ElseBlock),
        SwitchStatementNode @switch => @switch.Sections.Count > 0 &&
                                       @switch.Sections.All(section => section.Statements.LastOrDefault() is { } last && ReturnsOnAllPaths(last)),
        _ => false
    };

    private static bool IsEmptyArrayForArrayTarget(IAstNode? value, string? targetType) => value is ArrayLiteralExpressionNode { Elements.Count: 0 } && targetType?.EndsWith("[]", StringComparison.Ordinal) == true;

    private static void Report(DiagnosticCode code, string message, SourceLocation location, VerificationContext ctx) =>
        ctx.Diagnostics.Report(code, DiagnosticSeverity.Error, message, location);
}
