using System.Xml.Serialization;

namespace AionLightning.Game.Model.Templates.Challenge;

/// <summary>Port of Java model.templates.challenge.ChallengeReward — the task-completion reward
/// (distinct from the per-member <see cref="ContributionReward"/> ranking used by legion tasks).</summary>
public sealed class ChallengeReward
{
    [XmlAttribute("msg_id")] public int MsgId { get; set; }
    [XmlAttribute("value")]  public int Value { get; set; }
    [XmlAttribute("type")]   public RewardType Type { get; set; } = RewardType.NONE;
}
