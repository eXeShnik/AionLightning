namespace AionLightning.Game.Model.Templates.Challenge;

/// <summary>Port of Java model.templates.challenge.RewardType — what a completed challenge task's own
/// (non-contribution) reward does. SPAWN is a documented no-op, matching Java's own unfinished branch
/// (see Services/ChallengeTaskService.cs).</summary>
public enum RewardType
{
    NONE,
    POINT,
    SPAWN,
}
