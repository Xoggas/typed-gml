using TypedGML.Transpiler.Population.Models;

namespace TypedGML.Transpiler.Generation.Emitters;

/// <summary>
///     Emits @Object-decorated classes as GameMaker object event scripts + method scripts.
///     Per @Object class produces:
///     Objects/obj_Name/Create_0.gml       — field defaults + __types + ctor body
///     Objects/obj_Name/EventName_0.gml    — for each @NativeEvent method or override method
///     Scripts/obj_Name_MethodName/...gml  — for regular (non-event) methods
///     Scripts/obj_Name_Init/...gml        — if ctor has extra args beyond base() args
/// </summary>
public sealed class GameObjectEmitter : ICodeEmitter
{
    // Known GML event method names → event file prefixes
    private static readonly HashSet<string> KnownEvents = new(StringComparer.OrdinalIgnoreCase)
    {
        "Create", "Destroy", "Step", "Draw", "Alarm", "Collision",
        "KeyDown", "KeyUp", "KeyPressed", "MouseDown", "MouseUp",
        "MouseMoved", "BeginStep", "EndStep", "DrawGui", "PreDraw",
        "PostDraw", "RoomStart", "RoomEnd", "GameStart", "GameEnd",
        "CleanUp", "AsyncSystem", "AsyncHttp", "AsyncSaving"
    };

    public bool CanEmit(TgmlTypeDecl decl)
    {
        return decl is TgmlClassDecl cls && cls.IsGameObject;
    }

    public IEnumerable<GeneratedFile> Emit(TgmlTypeDecl decl, GenerationContext ctx)
    {
        var cls = (TgmlClassDecl)decl;
        ctx.CurrentType = cls;
        var objName = ctx.GmlObjectName(cls); // e.g. "obj_Player"
        var gmlName = cls.QualifiedName?.Replace(".", "_") ?? cls.Name;
        var stmtEmit = new StatementEmitter(ctx);

        // ── Create event ─────────────────────────────────────────────────────
        {
            var w = new GmlWriter();
            var typeIds = BuildTypesStruct(cls, ctx);
            w.WriteLine($"// Create event for {objName}");
            w.WriteLine($"__types = {{ {typeIds} }};");

            // Field defaults
            foreach (var field in cls.Fields)
            {
                if (field.IsStatic)
                {
                    var globalName = $"global.{gmlName}_{field.Name}";
                    var initVal = field.Initializer is not null
                        ? new ExpressionEmitter(ctx).Emit(field.Initializer)
                        : "undefined";
                    w.WriteLine($"{globalName} = {initVal};");
                }
                else
                {
                    var initVal = field.Initializer is not null
                        ? new ExpressionEmitter(ctx).Emit(field.Initializer)
                        : "undefined";
                    w.WriteLine($"{field.Name} = {initVal};");
                }
            }

            // Constructor body (base() call is handled by instance_create_layer, skip it)
            if (cls.Constructor?.Body is { } ctorBody)
            {
                stmtEmit.EmitBlock(ctorBody, w);
            }

            // Backing fields for auto-accessor properties
            foreach (var prop in cls.Properties)
            {
                if ((prop.Getter?.IsAuto == true || prop.Setter?.IsAuto == true) && !prop.IsGlobal)
                    w.WriteLine($"__backing_{prop.Name} = undefined;");
            }

            yield return new GeneratedFile($"Objects/{objName}/Create_0.gml", w.ToString());
        }

        // ── Method scripts + native event scripts ─────────────────────────────
        foreach (var method in cls.Methods)
        {
            if (method.IsAbstract || method.Body is null)
            {
                continue;
            }

            // Determine if this is a native event (explicit @NativeEvent or override of known event name)
            var isNativeEvent = method.Metadata.ContainsKey("NativeEventName") ||
                                (method.IsOverride && KnownEvents.Contains(method.Name));

            if (isNativeEvent)
            {
                var eventName = method.Metadata.TryGetValue("NativeEventName", out var ev)
                    ? (string)ev
                    : method.Name;

                var w = new GmlWriter();
                stmtEmit.EmitBlock(method.Body, w);
                yield return new GeneratedFile($"Objects/{objName}/{eventName}_0.gml", w.ToString());
            }
            else
            {
                // Regular method → script
                var scriptName = $"{objName}_{method.Name}";
                var w = new GmlWriter();
                var paramStr = string.Join(", ", method.Params.Select(p => p.Name));
                w.WriteLine($"function {scriptName}({paramStr})");
                w.OpenBrace();
                stmtEmit.EmitBlock(method.Body, w);
                w.CloseBrace();
                yield return new GeneratedFile($"Scripts/{scriptName}/{scriptName}.gml", w.ToString());
            }
        }

        // ── Properties → get/set scripts ──────────────────────────────────────
        foreach (var prop in cls.Properties)
        {
            ctx.CurrentPropertyName = prop.Name;
            ctx.IsGlobalProperty = prop.IsGlobal;

            if (prop.Getter is { } getter)
            {
                ctx.InsideGetter = true;
                var scriptName = $"{objName}_get_{prop.Name}";
                var w = new GmlWriter();
                w.WriteLine($"function {scriptName}()");
                w.OpenBrace();

                if (prop.IsGlobal && getter.IsAuto)
                {
                    w.WriteLine($"return global.{prop.Name};");
                }
                else if (getter.IsAuto)
                {
                    w.WriteLine($"return __backing_{prop.Name};");
                }
                else if (getter.Body is not null)
                {
                    stmtEmit.EmitBlock(getter.Body, w);
                }

                w.CloseBrace();
                yield return new GeneratedFile($"Scripts/{scriptName}/{scriptName}.gml", w.ToString());
            }

            if (prop.Setter is { } setter)
            {
                ctx.InsideSetter = true;
                var scriptName = $"{objName}_set_{prop.Name}";
                var w = new GmlWriter();
                w.WriteLine($"function {scriptName}(value)");
                w.OpenBrace();

                if (prop.IsGlobal && setter.IsAuto)
                {
                    w.WriteLine($"global.{prop.Name} = value;");
                }
                else if (setter.IsAuto)
                {
                    w.WriteLine($"__backing_{prop.Name} = value;");
                }
                else if (setter.Body is not null)
                {
                    stmtEmit.EmitBlock(setter.Body, w);
                }

                w.CloseBrace();
                yield return new GeneratedFile($"Scripts/{scriptName}/{scriptName}.gml", w.ToString());
            }

            ctx.InsideGetter = ctx.InsideSetter = false;
            ctx.CurrentPropertyName = null;
            ctx.IsGlobalProperty = false;
        }

        // ── Init script (extra constructor params beyond base args) ───────────
        if (cls.Constructor is { } ctor)
        {
            var baseArgCount = ctor.BaseArgs?.Count ?? 0;
            var extraParams = ctor.Params.Skip(baseArgCount).ToList();

            if (extraParams.Count > 0)
            {
                var initScriptName = $"{objName}_Init";
                var w = new GmlWriter();
                var paramStr = string.Join(", ", new[] { "inst" }.Concat(extraParams.Select(p => p.Name)));
                w.WriteLine($"function {initScriptName}({paramStr})");
                w.OpenBrace();
                w.WriteLine("with (inst)");
                w.OpenBrace();
                stmtEmit.EmitBlock(ctor.Body, w);
                w.CloseBrace();
                w.CloseBrace();
                yield return new GeneratedFile($"Scripts/{initScriptName}/{initScriptName}.gml", w.ToString());
            }
        }
    }

    private static string BuildTypesStruct(TgmlClassDecl cls, GenerationContext ctx)
    {
        var names = TypeHierarchyHelper.CollectAllGmlTypeNames(cls, ctx.TypeTable);
        return string.Join(", ", names.Select(n => $"__TYPE_{n}: true"));
    }
}