namespace TypedGML.Transpiler.Population.Models;

public enum AccessModifier
{
    Public,
    Protected,
    Private
}

public enum ClassModifier
{
    None,
    Abstract,
    Sealed,
    Virtual,
    Static
}

public enum ScopeModifier
{
    None,
    Static
}

/// <summary>Virtual / override / abstract / sealed modifier for methods and properties.</summary>
public enum VirtualModifier
{
    None,
    Virtual,
    Abstract,
    Override,
    Sealed
}

public enum ConversionModifier
{
    None,
    Implicit,
    Explicit
}

/// <summary>Parsed field modifier flags.</summary>
public sealed record FieldModifiers(
    AccessModifier Access,
    bool IsStatic,
    bool IsReadonly,
    bool IsConst);

/// <summary>Parsed property modifier flags.</summary>
public sealed record PropertyModifiers(
    AccessModifier Access,
    ScopeModifier Scope,
    VirtualModifier Virtual,
    bool IsReadonly = false);

/// <summary>Parsed method modifier flags.</summary>
public sealed record MethodModifiers(
    AccessModifier Access,
    bool IsStatic,
    VirtualModifier Virtual);
