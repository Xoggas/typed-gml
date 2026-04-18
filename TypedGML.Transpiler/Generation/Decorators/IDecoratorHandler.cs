using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Generation.Decorators;

/// <summary>
///     Handles a specific decorator applied to a type or member declaration.
///     Decorator handlers run before code emission and populate the Metadata dictionary.
/// </summary>
public interface IDecoratorHandler
{
    /// <summary>The decorator name this handler responds to (e.g. "Object").</summary>
    string DecoratorName { get; }

    /// <summary>Apply the decorator to a type declaration.</summary>
    void ApplyToType(TgmlDecorator decorator, TgmlTypeDecl type, TranspileContext ctx)
    {
    }

    /// <summary>Apply the decorator to a member declaration.</summary>
    void ApplyToMember(TgmlDecorator decorator, TgmlMemberDecl member, TranspileContext ctx)
    {
    }
}