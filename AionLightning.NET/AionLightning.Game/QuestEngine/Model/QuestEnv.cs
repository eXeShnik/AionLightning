using AionLightning.Game.Model;

namespace AionLightning.Game.QuestEngine.Model;

/// <summary>
/// Per-event context passed to quest handlers (Java QuestEngine.model.QuestEnv port).
/// <paramref name="RewardIndex"/> mirrors Java's <c>extendedRewardIndex</c> — the selectable
/// reward slot chosen by the client on SELECT_QUEST_REWARD.
/// </summary>
public sealed record QuestEnv(VisibleObject? Target, Player Player, int QuestId, int DialogId, int RewardIndex = 0)
{
    /// <summary>NPC template id of <see cref="Target"/>, or 0 when there is no NPC target (Java getTargetId()).</summary>
    public int TargetId => Target is Npc npc ? npc.Template.NpcId : 0;
}
