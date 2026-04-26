using TypedGML.Compiler.Ast;
using TypedGML.Compiler.Ast.Declarations;
using TypedGML.Compiler.Ast.Expressions;
using TypedGML.Compiler.Ast.Members;
using TypedGML.Compiler.Ast.Statements;
using TypedGML.Compiler.Diagnostics;
using TypedGML.Compiler.Symbols;

namespace TypedGML.Compiler.Verification;

public sealed class Verifier(IReadOnlyList<ISemanticCheck> checks, DiagnosticBag diagnostics)
{
    public void Verify(IReadOnlyList<FileNode> files, SymbolTable symbols)
    {
        var context = new VerificationContext(symbols, new ScopeStack(), diagnostics);
        context.Scope.Push();
        foreach (var file in files)
            Walk(file, context, string.Empty);
        context.Scope.Pop();
    }

    private void Walk(IAstNode node, VerificationContext ctx, string currentNamespace)
    {
        switch (node)
        {
            case FileNode file:
                RunChecks(file, ctx);
                WalkMany(file.TopLevelDeclarations, ctx, currentNamespace);
                break;
            case NamespaceDeclarationNode ns:
                RunChecks(ns, ctx);
                WalkMany(ns.Body, ctx, Combine(currentNamespace, ns.Name));
                break;
            case ClassDeclarationNode type:
                WithType(Combine(currentNamespace, type.Name), ctx, () => { RunChecks(type, ctx); WalkMany(type.Members, ctx, currentNamespace); });
                break;
            case StructDeclarationNode type:
                WithType(Combine(currentNamespace, type.Name), ctx, () => { RunChecks(type, ctx); WalkMany(type.Members, ctx, currentNamespace); });
                break;
            case InterfaceDeclarationNode type:
                WithType(Combine(currentNamespace, type.Name), ctx, () => { RunChecks(type, ctx); WalkMany(type.Members, ctx, currentNamespace); });
                break;
            case EnumDeclarationNode type:
                WithType(Combine(currentNamespace, type.Name), ctx, () => { RunChecks(type, ctx); WalkMany(type.Members, ctx, currentNamespace); });
                break;
            case EnumMemberNode member:
                RunChecks(member, ctx);
                WalkNullable(member.Value, ctx, currentNamespace);
                break;
            case FunctionDeclarationNode function:
                WithMember(new MemberSymbol { Name = function.Name, ReturnType = function.ReturnType, Modifiers = function.Modifiers.ToHashSet(StringComparer.Ordinal) }, ctx, false, function.Parameters, () => { RunChecks(function, ctx); WalkNullable(function.Body, ctx, currentNamespace); });
                break;
            case FieldDeclarationNode field:
                RunChecks(field, ctx);
                WalkNullable(field.Initializer, ctx, currentNamespace);
                break;
            case PropertyDeclarationNode property:
                WithMember(ResolveProperty(property), ctx, false, null, () => { RunChecks(property, ctx); foreach (var accessor in property.Accessors) WalkAccessor(accessor, property.TypeRef, property.Modifiers, ctx, currentNamespace); });
                break;
            case IndexerDeclarationNode indexer:
                WithMember(ResolveIndexer(indexer), ctx, false, [indexer.Parameter], () => { RunChecks(indexer, ctx); foreach (var accessor in indexer.Accessors) WalkAccessor(accessor, indexer.TypeRef, indexer.Modifiers, ctx, currentNamespace, indexer.Parameter); });
                break;
            case MethodDeclarationNode method:
                WithMember(FindMethod(method, ctx), ctx, false, method.Parameters, () => { RunChecks(method, ctx); WalkNullable(method.Body, ctx, currentNamespace); });
                break;
            case ConstructorDeclarationNode ctor:
                WithMember(FindConstructor(ctor, ctx), ctx, true, ctor.Parameters, () => { RunChecks(ctor, ctx); WalkMany(ctor.ChainArgs, ctx, currentNamespace); Walk(ctor.Body, ctx, currentNamespace); });
                break;
            case OperatorDeclarationNode op:
                WithMember(new MemberSymbol { Name = op.OperatorSymbol, ReturnType = op.ReturnType, Modifiers = op.Modifiers.ToHashSet(StringComparer.Ordinal) }, ctx, false, op.Parameters, () => { RunChecks(op, ctx); WalkNullable(op.Body, ctx, currentNamespace); });
                break;
            case ConversionOperatorNode op:
                WithMember(new MemberSymbol { Name = op.ConversionKind.ToString(), ReturnType = op.TargetType, Modifiers = op.Modifiers.ToHashSet(StringComparer.Ordinal) }, ctx, false, [op.Parameter], () => { RunChecks(op, ctx); WalkNullable(op.Body, ctx, currentNamespace); });
                break;
            default:
                RunChecks(node, ctx);
                WalkChildren(node, ctx, currentNamespace);
                break;
        }
    }

    private void RunChecks(IAstNode node, VerificationContext ctx)
    {
        foreach (var check in checks)
            if (check.Matches(node))
                check.Check(node, ctx);
    }

    private void WalkChildren(IAstNode node, VerificationContext ctx, string currentNamespace)
    {
        switch (node)
        {
            case ParameterNode parameter: WalkNullable(parameter.DefaultValue, ctx, currentNamespace); break;
            case DecoratorNode decorator: WalkMany(decorator.Args, ctx, currentNamespace); break;
            case BlockStatementNode block: ctx.Scope.Push(); WalkMany(block.Statements, ctx, currentNamespace); ctx.Scope.Pop(); break;
            case VarDeclarationStatementNode declaration: WalkNullable(declaration.Initializer, ctx, currentNamespace); ctx.Scope.Declare(declaration.Name, declaration.TypeRef ?? ExpressionTypeResolver.Resolve(declaration.Initializer, ctx) ?? string.Empty); break;
            case IfStatementNode @if: Walk(@if.Condition, ctx, currentNamespace); Walk(@if.ThenBlock, ctx, currentNamespace); WalkMany(@if.ElseIfClauses, ctx, currentNamespace); WalkNullable(@if.ElseBlock, ctx, currentNamespace); break;
            case ElseIfClauseNode clause: Walk(clause.Condition, ctx, currentNamespace); Walk(clause.ThenBlock, ctx, currentNamespace); break;
            case WhileStatementNode loop: WithLoop(ctx, () => { Walk(loop.Condition, ctx, currentNamespace); Walk(loop.Body, ctx, currentNamespace); }); break;
            case ForStatementNode loop: WithLoop(ctx, () => { WalkNullable(loop.Init, ctx, currentNamespace); WalkNullable(loop.Condition, ctx, currentNamespace); WalkMany(loop.Update, ctx, currentNamespace); Walk(loop.Body, ctx, currentNamespace); }); break;
            case RepeatStatementNode loop: WithLoop(ctx, () => { Walk(loop.Count, ctx, currentNamespace); Walk(loop.Body, ctx, currentNamespace); }); break;
            case SwitchStatementNode @switch: WithSwitch(ctx, () => { Walk(@switch.Value, ctx, currentNamespace); WalkMany(@switch.Sections, ctx, currentNamespace); }); break;
            case SwitchSectionNode section: WalkNullable(section.Label, ctx, currentNamespace); WalkMany(section.Statements, ctx, currentNamespace); break;
            case WithStatementNode with: Walk(with.Target, ctx, currentNamespace); Walk(with.Body, ctx, currentNamespace); break;
            case ReturnStatementNode ret: WalkNullable(ret.Value, ctx, currentNamespace); break;
            case TryStatementNode @try: Walk(@try.TryBlock, ctx, currentNamespace); WalkMany(@try.CatchClauses, ctx, currentNamespace); WalkNullable(@try.FinallyBlock, ctx, currentNamespace); break;
            case CatchClauseNode catchClause: ctx.Scope.Push(); ctx.Scope.Declare(catchClause.VariableName, catchClause.ExceptionType); Walk(catchClause.Body, ctx, currentNamespace); ctx.Scope.Pop(); break;
            case ThrowStatementNode thrown: Walk(thrown.Expression, ctx, currentNamespace); break;
            case ExpressionStatementNode expression: Walk(expression.Expression, ctx, currentNamespace); break;
            case BinaryExpressionNode binary: Walk(binary.Left, ctx, currentNamespace); Walk(binary.Right, ctx, currentNamespace); break;
            case UnaryExpressionNode unary: Walk(unary.Operand, ctx, currentNamespace); break;
            case TernaryExpressionNode ternary: Walk(ternary.Condition, ctx, currentNamespace); Walk(ternary.ThenExpr, ctx, currentNamespace); Walk(ternary.ElseExpr, ctx, currentNamespace); break;
            case AssignmentExpressionNode assignment: Walk(assignment.Target, ctx, currentNamespace); Walk(assignment.Value, ctx, currentNamespace); break;
            case MemberAccessExpressionNode access: Walk(access.Target, ctx, currentNamespace); break;
            case IndexerAccessExpressionNode indexer: Walk(indexer.Target, ctx, currentNamespace); Walk(indexer.Index, ctx, currentNamespace); break;
            case InvocationExpressionNode invocation: Walk(invocation.Target, ctx, currentNamespace); WalkMany(invocation.PositionalArgs, ctx, currentNamespace); WalkMany(invocation.NamedArgs, ctx, currentNamespace); break;
            case NamedArgNode named: Walk(named.Value, ctx, currentNamespace); break;
            case ObjectCreationExpressionNode creation: WalkMany(creation.PositionalArgs, ctx, currentNamespace); WalkMany(creation.NamedArgs, ctx, currentNamespace); break;
            case LambdaExpressionNode lambda: ctx.Scope.Push(); foreach (var parameter in lambda.Parameters) ctx.Scope.Declare(parameter.Name, parameter.TypeRef); Walk(lambda.Body, ctx, currentNamespace); ctx.Scope.Pop(); break;
            case CastExpressionNode cast: Walk(cast.Expression, ctx, currentNamespace); break;
            case NullCoalescingExpressionNode coalescing: Walk(coalescing.Left, ctx, currentNamespace); Walk(coalescing.Right, ctx, currentNamespace); break;
            case NullConditionalExpressionNode conditional: Walk(conditional.Target, ctx, currentNamespace); break;
            case ArrayLiteralExpressionNode array: WalkMany(array.Elements, ctx, currentNamespace); break;
            case DictionaryLiteralExpressionNode dictionary: WalkMany(dictionary.Entries, ctx, currentNamespace); break;
            case DictionaryEntryNode entry: Walk(entry.Key, ctx, currentNamespace); Walk(entry.Value, ctx, currentNamespace); break;
        }
    }

    private void WalkMany(IEnumerable<IAstNode> nodes, VerificationContext ctx, string currentNamespace) { foreach (var node in nodes) Walk(node, ctx, currentNamespace); }
    private void WalkNullable(IAstNode? node, VerificationContext ctx, string currentNamespace) { if (node is not null) Walk(node, ctx, currentNamespace); }
    private static string Combine(string currentNamespace, string name) => string.IsNullOrEmpty(currentNamespace) ? name : $"{currentNamespace}.{name}";
    private void WithType(string qualifiedName, VerificationContext ctx, Action action) { var previous = ctx.CurrentType; ctx.Symbols.TryResolve(qualifiedName, null, [], out var type); ctx.CurrentType = type; action(); ctx.CurrentType = previous; }
    private void WithMember(MemberSymbol member, VerificationContext ctx, bool isConstructor, IReadOnlyList<ParameterNode>? parameters, Action action) { var previousMember = ctx.CurrentMember; var previousConstructor = ctx.IsInConstructor; ctx.CurrentMember = member; ctx.IsInConstructor = isConstructor; ctx.Scope.Push(); foreach (var parameter in parameters ?? []) ctx.Scope.Declare(parameter.Name, parameter.TypeRef); action(); ctx.Scope.Pop(); ctx.CurrentMember = previousMember; ctx.IsInConstructor = previousConstructor; }
    private void WithLoop(VerificationContext ctx, Action action) { var previous = ctx.IsInLoop; ctx.IsInLoop = true; action(); ctx.IsInLoop = previous; }
    private void WithSwitch(VerificationContext ctx, Action action) { var previous = ctx.IsInSwitch; ctx.IsInSwitch = true; action(); ctx.IsInSwitch = previous; }
    private void WalkAccessor(AccessorNode accessor, string typeRef, IReadOnlyList<string> modifiers, VerificationContext ctx, string currentNamespace, ParameterNode? indexerParameter = null) { var member = new MemberSymbol { Name = accessor.Kind.ToString(), ReturnType = accessor.Kind == AccessorKind.Get ? typeRef : "void", Modifiers = modifiers.ToHashSet(StringComparer.Ordinal) }; WithMember(member, ctx, false, null, () => { if (indexerParameter is not null) ctx.Scope.Declare(indexerParameter.Name, indexerParameter.TypeRef); ctx.Scope.Declare("field", typeRef); if (accessor.Kind == AccessorKind.Set) ctx.Scope.Declare("value", typeRef); RunChecks(accessor, ctx); WalkNullable(accessor.Body, ctx, currentNamespace); }); }
    private MemberSymbol FindMethod(MethodDeclarationNode method, VerificationContext ctx) => SymbolResolver.FindMember(ctx.CurrentType, method.Name, out _) ?? new MemberSymbol { Name = method.Name, ReturnType = method.TypeRef, Parameters = method.Parameters.Select(p => new ParameterSymbol(p.Name, p.TypeRef, p.DefaultValue is not null, p.DefaultValue)).ToList(), Modifiers = method.Modifiers.ToHashSet(StringComparer.Ordinal) };
    private MemberSymbol FindConstructor(ConstructorDeclarationNode ctor, VerificationContext ctx) => ctx.CurrentType?.Members.FirstOrDefault(m => m.Kind == MemberKind.Constructor && ParametersMatch(m.Parameters, ctor.Parameters)) ?? new MemberSymbol { Name = ".ctor", ReturnType = "void", Parameters = ctor.Parameters.Select(p => new ParameterSymbol(p.Name, p.TypeRef, p.DefaultValue is not null, p.DefaultValue)).ToList(), Modifiers = ctor.Modifiers.ToHashSet(StringComparer.Ordinal) };
    private MemberSymbol ResolveProperty(PropertyDeclarationNode property) => new() { Name = property.Name, Kind = MemberKind.Property, ReturnType = property.TypeRef, Modifiers = property.Modifiers.ToHashSet(StringComparer.Ordinal) };
    private MemberSymbol ResolveIndexer(IndexerDeclarationNode indexer) => new() { Name = "this", Kind = MemberKind.Indexer, ReturnType = indexer.TypeRef, Parameters = [new ParameterSymbol(indexer.Parameter.Name, indexer.Parameter.TypeRef, indexer.Parameter.DefaultValue is not null, indexer.Parameter.DefaultValue)], Modifiers = indexer.Modifiers.ToHashSet(StringComparer.Ordinal) };
    private static bool ParametersMatch(IReadOnlyList<ParameterSymbol> symbols, IReadOnlyList<ParameterNode> parameters) => symbols.Count == parameters.Count && symbols.Zip(parameters).All(p => p.First.TypeRef == p.Second.TypeRef);
}
