namespace TypedGML.CLI.GameMaker;

internal static class EventTypeMap
{
    public static EventEntry Resolve(string gmlFileName)
    {
        if (gmlFileName.StartsWith("Collision_", StringComparison.Ordinal))
            return new EventEntry(4, 0, true, gmlFileName["Collision_".Length..]);

        if (TryResolveNumbered(gmlFileName, "Alarm_", 2, out var alarm))
            return alarm;
        if (TryResolveNumbered(gmlFileName, "Keyboard_", 5, out var keyboard))
            return keyboard;
        if (TryResolveNumbered(gmlFileName, "KeyPress_", 9, out var keyPress))
            return keyPress;
        if (TryResolveNumbered(gmlFileName, "KeyRelease_", 10, out var keyRelease))
            return keyRelease;

        return gmlFileName switch
        {
            "Create_0" => new EventEntry(0, 0, false, null),
            "Destroy_0" => new EventEntry(1, 0, false, null),
            "CleanUp_0" => new EventEntry(12, 0, false, null),
            "Step_0" => new EventEntry(3, 0, false, null),
            "Step_1" => new EventEntry(3, 1, false, null),
            "Step_2" => new EventEntry(3, 2, false, null),
            "Draw_0" => new EventEntry(8, 0, false, null),
            "Draw_64" => new EventEntry(8, 64, false, null),
            "Draw_65" => new EventEntry(8, 65, false, null),
            "Draw_66" => new EventEntry(8, 66, false, null),
            "Draw_72" => new EventEntry(8, 72, false, null),
            "Draw_73" => new EventEntry(8, 73, false, null),
            "Draw_76" => new EventEntry(8, 76, false, null),
            "Draw_77" => new EventEntry(8, 77, false, null),
            "Mouse_0" => new EventEntry(6, 0, false, null),
            "Mouse_1" => new EventEntry(6, 1, false, null),
            "Mouse_2" => new EventEntry(6, 2, false, null),
            "Mouse_3" => new EventEntry(6, 3, false, null),
            "Mouse_4" => new EventEntry(6, 4, false, null),
            "Mouse_5" => new EventEntry(6, 5, false, null),
            "Mouse_6" => new EventEntry(6, 6, false, null),
            "Mouse_7" => new EventEntry(6, 7, false, null),
            "Mouse_8" => new EventEntry(6, 8, false, null),
            "Mouse_9" => new EventEntry(6, 9, false, null),
            "Mouse_10" => new EventEntry(6, 10, false, null),
            "Mouse_11" => new EventEntry(6, 11, false, null),
            "Mouse_12" => new EventEntry(6, 12, false, null),
            "Mouse_50" => new EventEntry(6, 50, false, null),
            "Mouse_51" => new EventEntry(6, 51, false, null),
            "Mouse_52" => new EventEntry(6, 52, false, null),
            "Mouse_53" => new EventEntry(6, 53, false, null),
            "Mouse_54" => new EventEntry(6, 54, false, null),
            "Mouse_55" => new EventEntry(6, 55, false, null),
            "Mouse_56" => new EventEntry(6, 56, false, null),
            "Mouse_57" => new EventEntry(6, 57, false, null),
            "Mouse_58" => new EventEntry(6, 58, false, null),
            "Other_0" => new EventEntry(7, 0, false, null),
            "Other_1" => new EventEntry(7, 1, false, null),
            "Other_2" => new EventEntry(7, 2, false, null),
            "Other_3" => new EventEntry(7, 3, false, null),
            "Other_4" => new EventEntry(7, 4, false, null),
            "Other_5" => new EventEntry(7, 5, false, null),
            "Other_7" => new EventEntry(7, 7, false, null),
            "Other_8" => new EventEntry(7, 8, false, null),
            "Other_10" => new EventEntry(7, 10, false, null),
            "Other_11" => new EventEntry(7, 11, false, null),
            "Other_12" => new EventEntry(7, 12, false, null),
            "Other_13" => new EventEntry(7, 13, false, null),
            "Other_14" => new EventEntry(7, 14, false, null),
            "Other_15" => new EventEntry(7, 15, false, null),
            "Other_16" => new EventEntry(7, 16, false, null),
            "Other_17" => new EventEntry(7, 17, false, null),
            "Other_18" => new EventEntry(7, 18, false, null),
            "Other_19" => new EventEntry(7, 19, false, null),
            "Other_20" => new EventEntry(7, 20, false, null),
            "Other_21" => new EventEntry(7, 21, false, null),
            "Other_22" => new EventEntry(7, 22, false, null),
            "Other_23" => new EventEntry(7, 23, false, null),
            "Other_24" => new EventEntry(7, 24, false, null),
            "Other_25" => new EventEntry(7, 25, false, null),
            "Other_58" => new EventEntry(7, 58, false, null),
            "Other_62" => new EventEntry(7, 62, false, null),
            "Other_63" => new EventEntry(7, 63, false, null),
            "Other_68" => new EventEntry(7, 68, false, null),
            "Other_70" => new EventEntry(7, 70, false, null),
            "Other_74" => new EventEntry(7, 74, false, null),
            "Other_75" => new EventEntry(7, 75, false, null),
            "Other_80" => new EventEntry(7, 80, false, null),
            _ => throw new ArgumentException($"Unknown GML event file name '{gmlFileName}'.", nameof(gmlFileName)),
        };
    }

    private static bool TryResolveNumbered(
        string gmlFileName,
        string prefix,
        int eventType,
        out EventEntry entry)
    {
        if (gmlFileName.StartsWith(prefix, StringComparison.Ordinal) &&
            int.TryParse(gmlFileName[prefix.Length..], out var enumb))
        {
            entry = new EventEntry(eventType, enumb, false, null);
            return true;
        }

        entry = default!;
        return false;
    }

    public record EventEntry(
        int EventType,
        int Enumb,
        bool IsCollision,
        string? CollisionTargetName);
}
