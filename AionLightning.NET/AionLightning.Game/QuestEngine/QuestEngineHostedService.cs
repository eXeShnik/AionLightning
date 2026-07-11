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
    QuestRewardService rewardService,
    ILogger<QuestEngineHostedService> log) : IHostedService
{
    public Task StartAsync(CancellationToken ct)
    {
        foreach (var data in dataManager.QuestScripts.ItemCollecting)
            engine.AddQuestHandler(new ItemCollectingHandler(data, dataManager, questDao, rewardService));

        log.LogInformation("QuestEngine: registered {Count} quest handler(s) ({ItemCollecting} item_collecting)",
            engine.HandlerCount, dataManager.QuestScripts.ItemCollecting.Count);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
