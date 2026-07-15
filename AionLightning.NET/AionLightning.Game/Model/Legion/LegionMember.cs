namespace AionLightning.Game.Model.Legion;

public sealed class LegionMember
{
    public int ObjectId     { get; set; }
    public string Name      { get; set; } = "";
    public LegionRank Rank  { get; set; } = LegionRank.Legionary;
    public int ClassId      { get; set; }
    public byte Level       { get; set; }
    public int WorldId      { get; set; }
    public bool IsOnline    { get; set; }
    public string SelfIntro { get; set; } = "";
    public string Nickname  { get; set; } = "";

    /// <summary>Java LegionMember.challengeScore — running score toward the legion's current challenge
    /// task's per-member contribution-reward ranking (see Services/ChallengeTaskService.cs), reset to 0
    /// once rewards are distributed for a completed task.</summary>
    public int ChallengeScore { get; set; }
}
