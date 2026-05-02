namespace TypedGML.Compiler.Emission;

public static class GmlEventMap
{
    private static readonly Dictionary<string, string> _map;

    static GmlEventMap()
    {
        _map = new(StringComparer.Ordinal)
        {
            ["Create"]    = "Create_0",
            ["Destroy"]   = "Destroy_0",
            ["CleanUp"]   = "CleanUp_0",
            ["StepBegin"] = "Step_1",
            ["Step"]      = "Step_0",
            ["StepEnd"]   = "Step_2",
            ["DrawBegin"]    = "Draw_72",
            ["Draw"]         = "Draw_0",
            ["DrawEnd"]      = "Draw_73",
            ["DrawGui"]      = "Draw_64",
            ["DrawGuiBegin"] = "Draw_65",
            ["DrawGuiEnd"]   = "Draw_66",
            ["PreDraw"]      = "Draw_76",
            ["PostDraw"]     = "Draw_77",
            ["MouseLeft"]             = "Mouse_0",
            ["MouseRight"]            = "Mouse_1",
            ["MouseMiddle"]           = "Mouse_2",
            ["MouseLeftPressed"]      = "Mouse_3",
            ["MouseRightPressed"]     = "Mouse_4",
            ["MouseMiddlePressed"]    = "Mouse_5",
            ["MouseLeftReleased"]     = "Mouse_6",
            ["MouseRightReleased"]    = "Mouse_7",
            ["MouseMiddleReleased"]   = "Mouse_8",
            ["MouseWheelUp"]          = "Mouse_9",
            ["MouseWheelDown"]        = "Mouse_10",
            ["MouseEnter"]            = "Mouse_11",
            ["MouseLeave"]            = "Mouse_12",
            ["GlobalMouseLeft"]             = "Mouse_50",
            ["GlobalMouseRight"]            = "Mouse_51",
            ["GlobalMouseMiddle"]           = "Mouse_52",
            ["GlobalMouseLeftPressed"]      = "Mouse_53",
            ["GlobalMouseRightPressed"]     = "Mouse_54",
            ["GlobalMouseMiddlePressed"]    = "Mouse_55",
            ["GlobalMouseLeftReleased"]     = "Mouse_56",
            ["GlobalMouseRightReleased"]    = "Mouse_57",
            ["GlobalMouseMiddleReleased"]   = "Mouse_58",
            ["OutsideRoom"]       = "Other_0",
            ["IntersectBoundary"] = "Other_1",
            ["GameStart"]         = "Other_2",
            ["GameEnd"]           = "Other_3",
            ["RoomStart"]         = "Other_4",
            ["RoomEnd"]           = "Other_5",
            ["AnimationEnd"]      = "Other_7",
            ["AnimationUpdate"]   = "Other_58",
            ["PathEnded"]         = "Other_8",
            ["AsyncHttp"]          = "Other_62",
            ["AsyncAudioPlayback"] = "Other_74",
            ["AsyncDialog"]        = "Other_63",
            ["AsyncSaveLoad"]      = "Other_80",
            ["AsyncNetwork"]       = "Other_68",
            ["AsyncSocial"]        = "Other_70",
            ["AsyncSystem"]        = "Other_75",
            ["KeyPress"]        = "Keyboard_",
            ["KeyRelease"]      = "KeyRelease_",
        };

        for (var i = 0; i <= 11; i++)
            _map[$"Alarm{i}"] = $"Alarm_{i}";

        for (var i = 0; i <= 15; i++)
            _map[$"UserEvent{i}"] = $"Other_{10 + i}";
    }

    public static string Resolve(string logicalName, string? suffix = null)
    {
        if (!_map.TryGetValue(logicalName, out var resolved))
            throw new InvalidOperationException($"TGML0035: Unknown @NativeEvent name '{logicalName}'.");

        return suffix is null ? resolved : resolved + suffix;
    }
}
