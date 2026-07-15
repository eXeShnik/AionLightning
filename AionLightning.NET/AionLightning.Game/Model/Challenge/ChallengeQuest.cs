using AionLightning.Game.Model.Templates.Challenge;

namespace AionLightning.Game.Model.Challenge;

/// <summary>Port of Java model.challenge.ChallengeQuest — a challenge task's runtime progress toward one
/// of its declared quests.</summary>
public sealed class ChallengeQuest(ChallengeQuestTemplate template, int completeCount = 0)
{
    public ChallengeQuestTemplate Template { get; } = template;

    public int QuestId        => Template.Id;
    public int MaxRepeats      => Template.RepeatCount;
    public int ScorePerQuest  => Template.Score;

    public int CompleteCount { get; private set; } = completeCount;

    public void IncreaseCompleteCount() => CompleteCount++;
}
