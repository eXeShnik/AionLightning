using System.Xml.Serialization;

namespace AionLightning.Game.Model.Templates.Challenge;

/// <summary>
/// Port of Java model.templates.challenge.ContributionReward — one &lt;contrib&gt; tier of a legion
/// challenge task's completing-member reward, ranked by descending per-member challenge score. Document
/// order in challenge_tasks.xml is ascending by <see cref="Number"/> (a cumulative winner-count
/// threshold), which <see cref="Services.ChallengeTaskService"/> relies on directly rather than
/// re-sorting by <see cref="Rank"/>.
/// </summary>
public sealed class ContributionReward
{
    [XmlAttribute("item_count")] public int ItemCount { get; set; }
    [XmlAttribute("reward_id")]  public int RewardId   { get; set; }
    [XmlAttribute("number")]     public int Number     { get; set; }
    [XmlAttribute("rank")]       public int Rank       { get; set; }
}
