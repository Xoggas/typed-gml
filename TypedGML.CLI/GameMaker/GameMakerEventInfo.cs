namespace TypedGML.CLI.GameMaker;

internal sealed record GameMakerEventInfo(string GeneratedFileName, string PhysicalFileName, int EventType, int EventNum)
{
    private static readonly Dictionary<string, GameMakerEventInfo> KnownEvents = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Create_0.gml"] = new("Create_0.gml", "Create_0.gml", 0, 0),
        ["Destroy_0.gml"] = new("Destroy_0.gml", "Destroy_0.gml", 1, 0),
        ["Alarm_0.gml"] = new("Alarm_0.gml", "Alarm_0.gml", 2, 0),
        ["Step_0.gml"] = new("Step_0.gml", "Step_0.gml", 3, 0),
        ["BeginStep_0.gml"] = new("BeginStep_0.gml", "Step_1.gml", 3, 1),
        ["EndStep_0.gml"] = new("EndStep_0.gml", "Step_2.gml", 3, 2),
        ["Draw_0.gml"] = new("Draw_0.gml", "Draw_0.gml", 8, 0),
        ["GameStart_0.gml"] = new("GameStart_0.gml", "Other_2.gml", 7, 2),
        ["GameEnd_0.gml"] = new("GameEnd_0.gml", "Other_3.gml", 7, 3),
        ["RoomStart_0.gml"] = new("RoomStart_0.gml", "Other_4.gml", 7, 4),
        ["RoomEnd_0.gml"] = new("RoomEnd_0.gml", "Other_5.gml", 7, 5),
        ["DrawGui_0.gml"] = new("DrawGui_0.gml", "Draw_64.gml", 8, 64),
        ["PreDraw_0.gml"] = new("PreDraw_0.gml", "Draw_76.gml", 8, 76),
        ["PostDraw_0.gml"] = new("PostDraw_0.gml", "Draw_77.gml", 8, 77),
        ["CleanUp_0.gml"] = new("CleanUp_0.gml", "CleanUp_0.gml", 12, 0)
    };

    public static IReadOnlyCollection<GameMakerEventInfo> All => KnownEvents.Values;

    public static bool TryFromGeneratedFileName(string generatedFileName, out GameMakerEventInfo? info)
        => KnownEvents.TryGetValue(generatedFileName, out info);
}

