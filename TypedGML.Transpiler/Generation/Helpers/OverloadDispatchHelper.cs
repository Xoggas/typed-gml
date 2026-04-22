using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Generation.Helpers;

/// <summary>
///     Shared helpers for building runtime overload-dispatch conditions and
///     computing overload specificity scores used by both
///     <c>ScriptClassEmitter</c> and <c>GameObjectEmitter</c>.
/// </summary>
internal static class OverloadDispatchHelper
{
    // ── Method dispatch ───────────────────────────────────────────────────────

    /// <summary>
    ///     Builds a GML <c>if</c>-condition that selects <paramref name="overload"/>
    ///     from among all <paramref name="allOverloads"/> of the same name.
    ///     Uses <c>argument_count</c> and, when counts collide, a type-check on the
    ///     first distinguishing parameter position.
    /// </summary>
    public static string BuildMethodDispatchCondition(
        TgmlMethodDecl overload,
        IReadOnlyList<TgmlMethodDecl> allOverloads)
    {
        var count = overload.Params.Count;
        var sameCounts = allOverloads
            .Where(o => o != overload && o.Params.Count == count)
            .ToList();
        var conditions = new List<string> { $"argument_count == {count}" };

        if (sameCounts.Count > 0)
            AppendTypeDistinguisher(
                conditions,
                overload.Params,
                sameCounts.Select(o => o.Params).ToList(),
                argPrefix: "argument");

        return string.Join(" && ", conditions);
    }

    // ── Constructor dispatch ──────────────────────────────────────────────────

    /// <summary>
    ///     Builds a GML <c>if</c>-condition for selecting a constructor overload.
    ///     <paramref name="typeArgOffset"/> accounts for type-argument parameters
    ///     that are passed before user-visible constructor parameters.
    /// </summary>
    public static string BuildCtorDispatchCondition(
        TgmlConstructorDecl overload,
        IReadOnlyList<TgmlConstructorDecl> allOverloads,
        int typeArgOffset)
    {
        var count = overload.Params.Count;
        var sameCounts = allOverloads
            .Where(o => o != overload && o.Params.Count == count)
            .ToList();
        var conditions = new List<string> { $"argument_count == {count + typeArgOffset}" };

        if (sameCounts.Count > 0)
            AppendTypeDistinguisher(
                conditions,
                overload.Params,
                sameCounts.Select(o => o.Params).ToList(),
                argPrefix: "argument",
                offset: typeArgOffset);

        return string.Join(" && ", conditions);
    }

    // ── Specificity scoring ───────────────────────────────────────────────────

    /// <summary>
    ///     Specificity score for a method overload.
    ///     Higher score = should be dispatched before less-specific overloads.
    ///     <c>number</c>/<c>int</c>/<c>real</c> score 0 because GML cannot
    ///     distinguish them at runtime; <c>string</c>/<c>bool</c>/<c>array</c>
    ///     score highest; user-defined class/struct types score intermediate.
    /// </summary>
    public static int MethodSpecificity(TgmlMethodDecl m) =>
        m.Params.Sum(p => ParamTypeSpecificity(p.Type.Name.Full));

    /// <summary>Same as <see cref="MethodSpecificity"/> for constructor declarations.</summary>
    public static int CtorSpecificity(TgmlConstructorDecl c) =>
        c.Params.Sum(p => ParamTypeSpecificity(p.Type.Name.Full));

    /// <summary>
    ///     Returns a GML runtime type-check expression for <paramref name="tgmlType"/>
    ///     applied to <paramref name="argExpr"/>, or <c>null</c> when no reliable
    ///     check exists (i.e., numeric overloads that are indistinguishable at runtime).
    /// </summary>
    public static string? GmlTypeCheck(string tgmlType, string argExpr) =>
        tgmlType switch
        {
            "string" => $"is_string({argExpr})",
            "bool"   => $"is_bool({argExpr})",
            "array"  => $"is_array({argExpr})",
            "number" or "int" or "real" => null, // indistinguishable at GML runtime
            _ => $"is_struct({argExpr})"          // user-defined class / struct
        };

    // ── Internal helpers ──────────────────────────────────────────────────────

    /// <summary>
    ///     Appends a GML type-check condition at the first parameter position where
    ///     <paramref name="myParams"/> differs from all <paramref name="otherParams"/> lists.
    /// </summary>
    private static void AppendTypeDistinguisher(
        List<string> conditions,
        IReadOnlyList<TgmlParam> myParams,
        IReadOnlyList<IReadOnlyList<TgmlParam>> otherParams,
        string argPrefix,
        int offset = 0)
    {
        for (var i = 0; i < myParams.Count; i++)
        {
            var myType = myParams[i].Type.Name.Full;
            var conflictAtPos = otherParams.Any(o => o[i].Type.Name.Full == myType);
            if (conflictAtPos) continue;

            var check = GmlTypeCheck(myType, $"{argPrefix}[{i + offset}]");
            if (check is not null) conditions.Add(check);
            break;
        }
    }

    private static int ParamTypeSpecificity(string tgmlType) =>
        tgmlType switch
        {
            "string" or "bool" or "array" => 3,
            "number" or "int" or "real"   => 0,
            _ => 2 // user-defined class / struct
        };
}

