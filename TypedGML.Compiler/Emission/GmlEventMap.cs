namespace TypedGML.Compiler.Emission;

public static class GmlEventMap
{
    private static readonly Dictionary<string, string> Map = new(StringComparer.Ordinal)
    {
        ["Create"] = "Create_0",
        ["Destroy"] = "Destroy_0",
        ["Step"] = "Step_0",
        ["StepBegin"] = "Step_1",
        ["StepEnd"] = "Step_2",
        ["Draw"] = "Draw_0",
        ["DrawBegin"] = "Draw_72",
        ["DrawEnd"] = "Draw_73",
        ["DrawGui"] = "Draw_64",
        ["DrawGuiBegin"] = "Draw_65",
        ["DrawGuiEnd"] = "Draw_66",
        ["AsyncHttp"] = "Other_62",
        ["AsyncDialog"] = "Other_63",
        ["AsyncSaveLoad"] = "Other_80",
        ["RoomStart"] = "Other_4",
        ["RoomEnd"] = "Other_5",
        ["GameStart"] = "Other_2",
        ["GameEnd"] = "Other_3",
        ["AnimationEnd"] = "Other_7",
        ["CollisionPrefix"] = "Collision_",
        ["Alarm"] = "Alarm_",
        ["KeyPress"] = "Keyboard_",
        ["KeyRelease"] = "KeyRelease_",
        ["MouseLeft"] = "Mouse_0",
        ["MouseRight"] = "Mouse_1"
    };

    public static string Resolve(string logicalName, string? suffix = null)
    {
        if (!Map.TryGetValue(logicalName, out var resolved))
            throw new InvalidOperationException($"TGML0035: Unknown @NativeEvent name '{logicalName}'.");

        return suffix is null ? resolved : resolved + suffix;
    }
}
