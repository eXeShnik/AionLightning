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

    public Dictionary<int, LegionMember> Members { get; } = new();

    public int? BrigadeGeneralId =>
        Members.Values.FirstOrDefault(m => m.Rank == AionLightning.Game.Model.Legion.LegionRank.BrigadeGeneral)?.ObjectId;
}
