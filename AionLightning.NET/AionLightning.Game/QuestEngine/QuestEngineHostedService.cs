using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.QuestEngine.Handlers.Templates;
using AionLightning.Game.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.QuestEngine;

/// <summary>
/// Boots the quest engine at server start: builds one template handler per data-driven
/// quest_script_data entry and registers it into <see cref="QuestEngine"/>'s npc/quest indexes.
/// Must run before the game server starts accepting connections.
/// </summary>
public sealed class QuestEngineHostedService(
    QuestEngine engine,
    IDataManager dataManager,
    IQuestDao questDao,
    IItemDao itemDao,
    QuestRewardService rewardService,
    ILogger<QuestEngineHostedService> log) : IHostedService
{
    public Task StartAsync(CancellationToken ct)
    {
        foreach (var data in dataManager.QuestScripts.ItemCollecting)
            engine.AddQuestHandler(new ItemCollectingHandler(data, dataManager, questDao, rewardService));

        foreach (var data in dataManager.QuestScripts.MonsterHunt)
            engine.AddQuestHandler(new MonsterHuntHandler(data, dataManager, questDao, rewardService));

        foreach (var data in dataManager.QuestScripts.ReportTo)
            engine.AddQuestHandler(new ReportToHandler(data, dataManager, questDao, rewardService, itemDao));

        log.LogInformation(
            "QuestEngine: registered {Count} quest handler(s) ({ItemCollecting} item_collecting, {MonsterHunt} monster_hunt, {ReportTo} report_to)",
            engine.HandlerCount, dataManager.QuestScripts.ItemCollecting.Count,
            dataManager.QuestScripts.MonsterHunt.Count, dataManager.QuestScripts.ReportTo.Count);

        // Must run after every handler above has registered its NPCs, so the per-NPC
        // OnQuestStart index is complete before it's joined against the spawn table.
        engine.BuildWorldQuestIndex(dataManager.Spawns, dataManager.Quests);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
