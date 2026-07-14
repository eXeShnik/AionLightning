namespace AionLightning.Game.Model.AutoGroup;

/// <summary>
/// Per-queue capacity/timing/category constants — ported verbatim from Java's <c>AutoGroupType</c> enum
/// body (time/playerSize/difficultId per mask id). Only mask ids the Java enum actually declared are
/// present here: mask ids that exist only as rows in <c>auto_group.xml</c> (e.g. the npc-triggered
/// duplicate/legacy rows 16-20/99/100/201-206/401-407) have no entry, so
/// <see cref="Services.AutoGroupService"/> silently ignores registrations against them — exactly like
/// Java's <c>AutoGroupType.getAGTByMaskId</c> returning null for the same ids.
/// </summary>
public static class AutoGroupMetadata
{
    public readonly record struct Entry(byte PlayerSize, int TimeMs, byte DifficultyId, AutoGroupCategory Category);

    private static readonly Dictionary<int, Entry> ByMaskId = new()
    {
        // Dredgion
        [1] = new(12, 600000, 0, AutoGroupCategory.Dredgion),
        [2] = new(12, 600000, 0, AutoGroupCategory.Dredgion),
        [3] = new(12, 600000, 0, AutoGroupCategory.Dredgion),
        // Co-op instances (AutoGeneralInstance)
        [4]  = new(6, 300000, 0, AutoGroupCategory.General),
        [5]  = new(6, 600000, 0, AutoGroupCategory.General),
        [6]  = new(6, 1200000, 0, AutoGroupCategory.General),
        [7]  = new(6, 1200000, 0, AutoGroupCategory.General),
        [8]  = new(6, 600000, 0, AutoGroupCategory.General),
        [9]  = new(6, 600000, 0, AutoGroupCategory.General),
        [11] = new(6, 600000, 0, AutoGroupCategory.General),
        [14] = new(6, 300000, 0, AutoGroupCategory.General),
        // PvP FFA arenas (AutoPvPFFAInstance)
        [21] = new(8, 110000, 1, AutoGroupCategory.PvpFfa),
        [22] = new(8, 110000, 2, AutoGroupCategory.PvpFfa),
        [23] = new(8, 110000, 3, AutoGroupCategory.PvpFfa),
        [39] = new(8, 110000, 4, AutoGroupCategory.PvpFfa),
        [27] = new(8, 110000, 1, AutoGroupCategory.PvpFfa),
        [28] = new(8, 110000, 2, AutoGroupCategory.PvpFfa),
        [29] = new(8, 110000, 3, AutoGroupCategory.PvpFfa),
        [43] = new(8, 110000, 4, AutoGroupCategory.PvpFfa),
        // PvP solo-duel arenas (AutoPvPFFAInstance, 2-seat)
        [24] = new(2, 110000, 1, AutoGroupCategory.PvpSolo),
        [25] = new(2, 110000, 2, AutoGroupCategory.PvpSolo),
        [26] = new(2, 110000, 3, AutoGroupCategory.PvpSolo),
        [40] = new(2, 110000, 4, AutoGroupCategory.PvpSolo),
        [30] = new(2, 110000, 1, AutoGroupCategory.PvpSolo),
        [31] = new(2, 110000, 2, AutoGroupCategory.PvpSolo),
        [32] = new(2, 110000, 3, AutoGroupCategory.PvpSolo),
        [44] = new(2, 110000, 4, AutoGroupCategory.PvpSolo),
        // Harmony arenas (AutoHarmonyInstance)
        [33]  = new(6, 110000, 1, AutoGroupCategory.Harmony),
        [34]  = new(6, 110000, 2, AutoGroupCategory.Harmony),
        [35]  = new(6, 110000, 3, AutoGroupCategory.Harmony),
        [41]  = new(6, 110000, 3, AutoGroupCategory.Harmony), // Java source itself reuses difficultId 3 here
        [45]  = new(6, 110000, 4, AutoGroupCategory.Harmony),
        [101] = new(6, 110000, 1, AutoGroupCategory.Harmony),
        [102] = new(6, 110000, 2, AutoGroupCategory.Harmony),
        [103] = new(6, 110000, 3, AutoGroupCategory.Harmony),
        [104] = new(4, 110000, 1, AutoGroupCategory.Harmony),
        [105] = new(4, 110000, 2, AutoGroupCategory.Harmony),
        [106] = new(4, 110000, 3, AutoGroupCategory.Harmony),
        // Glory arenas
        [38] = new(4, 110000, 1, AutoGroupCategory.Glory),
        [42] = new(4, 110000, 2, AutoGroupCategory.Glory),
        // Faction-vs-faction battlefields
        [107] = new(12, 600000, 0, AutoGroupCategory.Kamar),
        [108] = new(12, 600000, 0, AutoGroupCategory.Ophidan),
        [109] = new(12, 600000, 0, AutoGroupCategory.IronWall),
        // Remaining co-op instances (AutoGeneralInstance)
        [201] = new(6, 600000, 0, AutoGroupCategory.General),
        [206] = new(6, 600000, 0, AutoGroupCategory.General),
        [302] = new(6, 300000, 0, AutoGroupCategory.General),
        [303] = new(6, 600000, 0, AutoGroupCategory.General),
        [304] = new(6, 1200000, 0, AutoGroupCategory.General),
        [305] = new(6, 1200000, 0, AutoGroupCategory.General),
        [306] = new(6, 1200000, 0, AutoGroupCategory.General),
        [307] = new(6, 1200000, 0, AutoGroupCategory.General),
        [308] = new(6, 1200000, 0, AutoGroupCategory.General),
        [309] = new(6, 600000, 0, AutoGroupCategory.General),
        [310] = new(6, 600000, 0, AutoGroupCategory.General),
        [311] = new(6, 600000, 0, AutoGroupCategory.General),
        [312] = new(6, 600000, 0, AutoGroupCategory.General),
        [313] = new(6, 600000, 0, AutoGroupCategory.General),
        [314] = new(6, 600000, 0, AutoGroupCategory.General),
        [315] = new(6, 600000, 0, AutoGroupCategory.General),
        [316] = new(6, 600000, 0, AutoGroupCategory.General),
        [317] = new(6, 600000, 0, AutoGroupCategory.General),
        [318] = new(6, 600000, 0, AutoGroupCategory.General),
        [319] = new(6, 600000, 0, AutoGroupCategory.General),
        [320] = new(6, 600000, 0, AutoGroupCategory.General),
        [321] = new(6, 600000, 0, AutoGroupCategory.General),
        [322] = new(6, 600000, 0, AutoGroupCategory.General),
        [323] = new(6, 600000, 0, AutoGroupCategory.General),
        [324] = new(6, 600000, 0, AutoGroupCategory.General),
        [325] = new(6, 600000, 0, AutoGroupCategory.General),
        [326] = new(6, 600000, 0, AutoGroupCategory.General),
        [327] = new(3, 600000, 0, AutoGroupCategory.General),
        [328] = new(3, 600000, 0, AutoGroupCategory.General),
        [329] = new(3, 600000, 0, AutoGroupCategory.General),
        [330] = new(6, 600000, 0, AutoGroupCategory.General),
        [331] = new(6, 600000, 0, AutoGroupCategory.General),
        [332] = new(6, 600000, 0, AutoGroupCategory.General),
        [333] = new(6, 600000, 0, AutoGroupCategory.General),
        [334] = new(6, 600000, 0, AutoGroupCategory.General),
        [335] = new(6, 600000, 0, AutoGroupCategory.General),
        [336] = new(6, 600000, 0, AutoGroupCategory.General),
        [337] = new(6, 600000, 0, AutoGroupCategory.General),
        [338] = new(6, 600000, 0, AutoGroupCategory.General),
        [339] = new(6, 600000, 0, AutoGroupCategory.General),
        [340] = new(6, 600000, 0, AutoGroupCategory.General),
        [341] = new(6, 600000, 0, AutoGroupCategory.General),
    };

    public static bool TryGet(int maskId, out Entry entry) => ByMaskId.TryGetValue(maskId, out entry);
}
