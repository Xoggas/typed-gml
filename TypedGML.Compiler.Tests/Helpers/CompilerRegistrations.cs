using TypedGML.Compiler.Emission;
using TypedGML.Compiler.Emission.Emitters;
using TypedGML.Compiler.Emission.Emitters.Expressions;
using TypedGML.Compiler.Emission.Emitters.Statements;
using TypedGML.Compiler.Verification;
using TypedGML.Compiler.Verification.Checks;

namespace TypedGML.Compiler.Tests.Helpers;

internal static class CompilerRegistrations
{
    public static IReadOnlyList<ISemanticCheck> Checks() =>
    [
        new TypeAssignabilityCheck(),
        new NullabilityCheck(),
        new MemberAccessCheck(),
        new BaseCallCheck(),
        new MethodCallCheck(),
        new ConstructorDeclarationCheck(),
        new ConstructorCallCheck(),
        new OperatorCheck(),
        new ControlFlowCheck(),
        new SealedInheritanceCheck(),
        new AbstractCompletenessCheck(),
        new InterfaceImplementationCheck(),
        new OverrideSignatureCheck(),
        new StructInheritanceCheck(),
        new GenericConstraintCheck(),
        new DelegateSignatureCheck(),
        new EventAccessCheck(),
        new LambdaCheck(),
        new DecoratorPlacementCheck(),
        new NativeEventNameCheck(),
        new CollisionDecoratorCheck(),
        new ObjectDecoratorCheck(),
        new GameObjectDecoratorCheck(),
        new ConstExpressionCheck(),
        new ReadonlyAssignmentCheck(),
        new StaticModifierCheck(),
        new StaticConstructorCheck(),
        new WithTargetCheck(),
        new SwitchCaseConstantCheck(),
        new DuplicateCaseCheck(),
        new VarInferenceCheck(),
        new ArrayLiteralTypeCheck(),
        new ThrowTypeCheck(),
        new TypeofNameCheck(),
        new IsAsCompatibilityCheck(),
        new DuplicateMemberCheck(),
        new DuplicateParameterCheck(),
        new DefaultParameterConstCheck(),
        new ObjectCreationCheck(),
    ];

    public static IReadOnlyList<INodeEmitter> Emitters()
    {
        var staticCtorEmitter = new StaticCtorEmitter();
        return
        [
            new ClassEmitter(staticCtorEmitter), new StructEmitter(staticCtorEmitter), new EnumEmitter(),
            new DelegateEmitter(), new FunctionEmitter(), new ConstructorEmitter(), new MethodEmitter(),
            new PropertyEmitter(), new IndexerEmitter(), new OperatorEmitter(), new FieldEmitter(), new EventEmitter(),
            new BlockStatementEmitter(), new IfStatementEmitter(), new WhileStatementEmitter(),
            new ForStatementEmitter(), new RepeatStatementEmitter(), new SwitchStatementEmitter(),
            new WithStatementEmitter(), new TryStatementEmitter(), new ReturnStatementEmitter(),
            new ThrowStatementEmitter(), new VarDeclarationStatementEmitter(), new ExpressionStatementEmitter(),
            new BinaryExpressionEmitter(), new UnaryExpressionEmitter(), new TernaryExpressionEmitter(),
            new AssignmentExpressionEmitter(), new MemberAccessExpressionEmitter(), new InvocationExpressionEmitter(),
            new IndexerAccessExpressionEmitter(), new ObjectCreationExpressionEmitter(), new LambdaExpressionEmitter(),
            new NullCoalescingExpressionEmitter(), new NullConditionalExpressionEmitter(), new ArrayLiteralExpressionEmitter(),
            new DictionaryLiteralExpressionEmitter(), new TypeofExpressionEmitter(), new NameofExpressionEmitter(),
            new DefaultExpressionEmitter(),
        ];
    }
}
