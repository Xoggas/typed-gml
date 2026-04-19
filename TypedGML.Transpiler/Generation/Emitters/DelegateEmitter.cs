using TypedGML.Transpiler.Checking;
using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Generation.Emitters;

public sealed class DelegateEmitter : ICodeEmitter
{
    public bool CanEmit(TgmlTypeDecl decl)
    {
        return decl is TgmlDelegateDecl;
    }

    public IEnumerable<GeneratedFile> Emit(TgmlTypeDecl decl, GenerationContext ctx)
    {
        if (decl is not TgmlDelegateDecl delegateDecl)
            yield break;

        var runtimeName = DelegateFacts.GetRuntimeTypeName(delegateDecl);
        var writer = new GmlWriter();
        var invokeParams = delegateDecl.Params.Select((_, index) => $"__arg{index}").ToList();
        var invokeArgs = string.Join(", ", invokeParams);
        var invokeCallArgs = invokeParams.Count == 0 ? string.Empty : $", {invokeArgs}";

        writer.WriteLine($"function {runtimeName}(__handlers) constructor");
        writer.OpenBrace();
        writer.WriteLine("__invocationList = is_array(__handlers) ? __handlers : [];");
        writer.CloseBrace();
        writer.WriteLine();

        writer.WriteLine($"function {runtimeName}__create(__handler)");
        writer.OpenBrace();
        writer.WriteLine("if (is_undefined(__handler))");
        writer.OpenBrace();
        writer.WriteLine("return undefined;");
        writer.CloseBrace();
        writer.WriteLine();
        writer.WriteLine("if (is_struct(__handler) && variable_struct_exists(__handler, \"__invocationList\"))");
        writer.OpenBrace();
        writer.WriteLine("return __handler;");
        writer.CloseBrace();
        writer.WriteLine();
        writer.WriteLine($"return new {runtimeName}([__handler]);");
        writer.CloseBrace();
        writer.WriteLine();

        writer.WriteLine($"function {runtimeName}__combine(__left, __right)");
        writer.OpenBrace();
        writer.WriteLine("if (is_undefined(__left))");
        writer.OpenBrace();
        writer.WriteLine("return __right;");
        writer.CloseBrace();
        writer.WriteLine();
        writer.WriteLine("if (is_undefined(__right))");
        writer.OpenBrace();
        writer.WriteLine("return __left;");
        writer.CloseBrace();
        writer.WriteLine();
        writer.WriteLine("var __handlers = [];");
        writer.WriteLine("var __leftHandlers = __left.__invocationList;");
        writer.WriteLine("for (var __i = 0; __i < array_length(__leftHandlers); __i++)");
        writer.OpenBrace();
        writer.WriteLine("array_push(__handlers, __leftHandlers[__i]);");
        writer.CloseBrace();
        writer.WriteLine();
        writer.WriteLine("var __rightHandlers = __right.__invocationList;");
        writer.WriteLine("for (var __i = 0; __i < array_length(__rightHandlers); __i++)");
        writer.OpenBrace();
        writer.WriteLine("array_push(__handlers, __rightHandlers[__i]);");
        writer.CloseBrace();
        writer.WriteLine();
        writer.WriteLine($"return new {runtimeName}(__handlers);");
        writer.CloseBrace();
        writer.WriteLine();

        writer.WriteLine($"function {runtimeName}__remove(__left, __right)");
        writer.OpenBrace();
        writer.WriteLine("if (is_undefined(__left))");
        writer.OpenBrace();
        writer.WriteLine("return undefined;");
        writer.CloseBrace();
        writer.WriteLine();
        writer.WriteLine("if (is_undefined(__right))");
        writer.OpenBrace();
        writer.WriteLine("return __left;");
        writer.CloseBrace();
        writer.WriteLine();
        writer.WriteLine("var __leftHandlers = __left.__invocationList;");
        writer.WriteLine("var __rightHandlers = __right.__invocationList;");
        writer.WriteLine("var __leftLength = array_length(__leftHandlers);");
        writer.WriteLine("var __rightLength = array_length(__rightHandlers);");
        writer.WriteLine("if (__rightLength == 0 || __rightLength > __leftLength)");
        writer.OpenBrace();
        writer.WriteLine("return __left;");
        writer.CloseBrace();
        writer.WriteLine();
        writer.WriteLine("for (var __start = __leftLength - __rightLength; __start >= 0; __start--)");
        writer.OpenBrace();
        writer.WriteLine("var __matches = true;");
        writer.WriteLine("for (var __offset = 0; __offset < __rightLength; __offset++)");
        writer.OpenBrace();
        writer.WriteLine("if (__leftHandlers[__start + __offset] != __rightHandlers[__offset])");
        writer.OpenBrace();
        writer.WriteLine("__matches = false;");
        writer.WriteLine("break;");
        writer.CloseBrace();
        writer.CloseBrace();
        writer.WriteLine();
        writer.WriteLine("if (__matches)");
        writer.OpenBrace();
        writer.WriteLine("var __handlers = [];");
        writer.WriteLine("for (var __i = 0; __i < __start; __i++)");
        writer.OpenBrace();
        writer.WriteLine("array_push(__handlers, __leftHandlers[__i]);");
        writer.CloseBrace();
        writer.WriteLine();
        writer.WriteLine("for (var __i = __start + __rightLength; __i < __leftLength; __i++)");
        writer.OpenBrace();
        writer.WriteLine("array_push(__handlers, __leftHandlers[__i]);");
        writer.CloseBrace();
        writer.WriteLine();
        writer.WriteLine("if (array_length(__handlers) == 0)");
        writer.OpenBrace();
        writer.WriteLine("return undefined;");
        writer.CloseBrace();
        writer.WriteLine();
        writer.WriteLine($"return new {runtimeName}(__handlers);");
        writer.CloseBrace();
        writer.CloseBrace();
        writer.WriteLine();
        writer.WriteLine("return __left;");
        writer.CloseBrace();
        writer.WriteLine();

        writer.WriteLine($"function {runtimeName}__invoke(__delegate{invokeCallArgs})");
        writer.OpenBrace();
        writer.WriteLine("if (is_undefined(__delegate))");
        writer.OpenBrace();
        writer.WriteLine("return undefined;");
        writer.CloseBrace();
        writer.WriteLine();
        writer.WriteLine("var __handlers = __delegate.__invocationList;");
        writer.WriteLine("var __result = undefined;");
        writer.WriteLine("for (var __i = 0; __i < array_length(__handlers); __i++)");
        writer.OpenBrace();
        writer.WriteLine($"__result = __handlers[__i]({invokeArgs});".Replace("()", "()"));
        writer.CloseBrace();
        writer.WriteLine();
        writer.WriteLine("return __result;");
        writer.CloseBrace();

        yield return new GeneratedFile($"Scripts/{runtimeName}/{runtimeName}.gml", writer.ToString());
    }
}
