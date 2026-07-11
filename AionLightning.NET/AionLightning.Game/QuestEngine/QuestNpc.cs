namespace AionLightning.Game.QuestEngine;

/// <summary>
/// Per-NPC quest index (Java <c>questEngine.model.QuestNpc</c> port): which quests this NPC
/// participates in for each event type. Quest ids are added by template handlers during
/// <see cref="Handlers.IQuestHandler.Register"/>.
/// </summary>
public sealed class QuestNpc(int npcId)
{
    public int NpcId { get; } = npcId;

    public List<int> OnQuestStart { get; } = [];
    public List<int> OnTalk       { get; } = [];
    public List<int> OnKill       { get; } = [];
}
