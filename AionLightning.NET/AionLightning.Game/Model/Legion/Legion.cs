namespace AionLightning.Game.Model.Legion;

public sealed class Legion
{
    public int LegionId      { get; set; }
    public string Name       { get; set; } = "";
    public int Level         { get; set; } = 1;
    public int LegionRank    { get; set; }
    public long ContributionPoints { get; set; }
    public string Announcement { get; set; } = "";
    public short DeputyPermission    { get; set; }
    public short CenturionPermission { get; set; }
    public short LegionaryPermission { get; set; }
    public short VolunteerPermission { get; set; }

    public long WarehouseKinah { get; set; }

    // Emblem — in-memory only (not persisted); 0=DEFAULT, 1=CUSTOM
    public byte EmblemId   { get; set; }
    public byte EmblemType { get; set; }
    public byte EmblemR    { get; set; }
    public byte EmblemG    { get; set; }
    public byte EmblemB    { get; set; }

    public Dictionary<int, LegionMember> Members { get; } = new();

    public int? BrigadeGeneralId =>
        Members.Values.FirstOrDefault(m => m.Rank == AionLightning.Game.Model.Legion.LegionRank.BrigadeGeneral)?.ObjectId;
}
