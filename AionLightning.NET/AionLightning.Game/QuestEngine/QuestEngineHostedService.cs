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
    IRecipeDao recipeDao,
    QuestRewardService rewardService,
    SpawnService spawnService,
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

        foreach (var data in dataManager.QuestScripts.ReportToMany)
            engine.AddQuestHandler(new ReportToManyHandler(data, dataManager, questDao, rewardService, itemDao));

        foreach (var data in dataManager.QuestScripts.KillInWorld)
            engine.AddQuestHandler(new KillInWorldHandler(data, dataManager, questDao, rewardService));

        foreach (var data in dataManager.QuestScripts.KillSpawned)
            engine.AddQuestHandler(new KillSpawnedHandler(data, dataManager, questDao, rewardService, spawnService));

        foreach (var data in dataManager.QuestScripts.WorkOrders)
            engine.AddQuestHandler(new WorkOrdersHandler(data, dataManager, questDao, rewardService, itemDao, recipeDao));

        log.LogInformation(
            "QuestEngine: registered {Count} quest handler(s) ({ItemCollecting} item_collecting, {MonsterHunt} monster_hunt, " +
            "{ReportTo} report_to, {ReportToMany} report_to_many, {KillInWorld} kill_in_world, {KillSpawned} kill_spawned, {WorkOrders} work_order)",
            engine.HandlerCount, dataManager.QuestScripts.ItemCollecting.Count,
            dataManager.QuestScripts.MonsterHunt.Count, dataManager.QuestScripts.ReportTo.Count,
            dataManager.QuestScripts.ReportToMany.Count, dataManager.QuestScripts.KillInWorld.Count,
            dataManager.QuestScripts.KillSpawned.Count, dataManager.QuestScripts.WorkOrders.Count);

        // Must run after every handler above has registered its NPCs, so the per-NPC
        // OnQuestStart index is complete before it's joined against the spawn table.
        engine.BuildWorldQuestIndex(dataManager.Spawns, dataManager.Quests);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
