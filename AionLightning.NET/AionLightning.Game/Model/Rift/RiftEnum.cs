namespace AionLightning.Game.Model.Rift;

/// <summary>
/// Java services.rift.RiftEnum — static per-rift definition: the master/slave spawn anchor
/// names (resolved against <see cref="AionLightning.Game.DataHolders.RiftSpawnData"/>), the max
/// concurrent entries, the entry level range, and the destination race. <see cref="Id"/> matches
/// the id used by <see cref="RiftTemplate"/>/rift_locations.xml and rift_schedule.xml.
/// note: Java's RiftEnum also carried two vortex entries (KAISINEL_AM id 1170, MARCHUTAN_AM id
/// 1280) — those belong to the separate Dimensional Vortex subsystem (VortexService/VortexLocation,
/// its own data source, not rift_locations.xml) and are out of scope here; only the 36 general
/// abyss-invasion rifts (Eltnen/Heiron/Inggison &lt;-&gt; Morheim/Beluslan/Gelkmaros) are ported.
/// </summary>
public sealed record RiftEnum(int Id, string Master, string Slave, int Entries, int MinLevel, int MaxLevel, Race Destination)
{
    public static readonly IReadOnlyList<RiftEnum> All =
    [
        new(2120, "ELTNEN_AM", "MORHEIM_AS", 12, 20, 28, Race.ASMODIANS),
        new(2121, "ELTNEN_BM", "MORHEIM_BS", 20, 20, 32, Race.ASMODIANS),
        new(2122, "ELTNEN_CM", "MORHEIM_CS", 35, 20, 36, Race.ASMODIANS),
        new(2123, "ELTNEN_DM", "MORHEIM_DS", 35, 20, 37, Race.ASMODIANS),
        new(2124, "ELTNEN_EM", "MORHEIM_ES", 45, 20, 40, Race.ASMODIANS),
        new(2125, "ELTNEN_FM", "MORHEIM_FS", 50, 20, 40, Race.ASMODIANS),
        new(2126, "ELTNEN_GM", "MORHEIM_GS", 50, 20, 45, Race.ASMODIANS),
        new(2140, "HEIRON_AM", "BELUSLAN_AS", 24, 30, 38, Race.ASMODIANS),
        new(2141, "HEIRON_BM", "BELUSLAN_BS", 36, 30, 42, Race.ASMODIANS),
        new(2142, "HEIRON_CM", "BELUSLAN_CS", 48, 30, 46, Race.ASMODIANS),
        new(2143, "HEIRON_DM", "BELUSLAN_DS", 48, 30, 40, Race.ASMODIANS),
        new(2144, "HEIRON_EM", "BELUSLAN_ES", 60, 30, 50, Race.ASMODIANS),
        new(2145, "HEIRON_FM", "BELUSLAN_FS", 72, 30, 50, Race.ASMODIANS),
        new(2146, "HEIRON_GM", "BELUSLAN_GS", 72, 30, 50, Race.ASMODIANS),
        new(2150, "INGGISON_AM", "GELKMAROS_AS", 150, 20, 60, Race.ASMODIANS),
        new(2151, "INGGISON_BM", "GELKMAROS_BS", 150, 20, 60, Race.ASMODIANS),
        new(2152, "INGGISON_CM", "GELKMAROS_CS", 150, 20, 60, Race.ASMODIANS),
        new(2153, "INGGISON_DM", "GELKMAROS_DS", 150, 20, 60, Race.ASMODIANS),
        new(2220, "MORHEIM_AM", "ELTNEN_AS", 12, 20, 28, Race.ELYOS),
        new(2221, "MORHEIM_BM", "ELTNEN_BS", 20, 20, 32, Race.ELYOS),
        new(2222, "MORHEIM_CM", "ELTNEN_CS", 35, 20, 36, Race.ELYOS),
        new(2223, "MORHEIM_DM", "ELTNEN_DS", 35, 20, 37, Race.ELYOS),
        new(2224, "MORHEIM_EM", "ELTNEN_ES", 45, 20, 40, Race.ELYOS),
        new(2225, "MORHEIM_FM", "ELTNEN_FS", 50, 20, 40, Race.ELYOS),
        new(2226, "MORHEIM_GM", "ELTNEN_GS", 50, 20, 45, Race.ELYOS),
        new(2240, "BELUSLAN_AM", "HEIRON_AS", 24, 30, 38, Race.ELYOS),
        new(2241, "BELUSLAN_BM", "HEIRON_BS", 36, 30, 42, Race.ELYOS),
        new(2242, "BELUSLAN_CM", "HEIRON_CS", 48, 30, 46, Race.ELYOS),
        new(2243, "BELUSLAN_DM", "HEIRON_DS", 48, 30, 40, Race.ELYOS),
        new(2244, "BELUSLAN_EM", "HEIRON_ES", 60, 30, 50, Race.ELYOS),
        new(2245, "BELUSLAN_FM", "HEIRON_FS", 72, 30, 50, Race.ELYOS),
        new(2246, "BELUSLAN_GM", "HEIRON_GS", 72, 30, 50, Race.ELYOS),
        new(2270, "GELKMAROS_AM", "INGGISON_AS", 150, 20, 60, Race.ELYOS),
        new(2271, "GELKMAROS_BM", "INGGISON_BS", 150, 20, 60, Race.ELYOS),
        new(2272, "GELKMAROS_CM", "INGGISON_CS", 150, 20, 60, Race.ELYOS),
        new(2273, "GELKMAROS_DM", "INGGISON_DS", 150, 20, 60, Race.ELYOS),
    ];

    private static readonly Dictionary<int, RiftEnum> ById = All.ToDictionary(r => r.Id);

    public static RiftEnum? GetById(int id) => ById.GetValueOrDefault(id);
}
