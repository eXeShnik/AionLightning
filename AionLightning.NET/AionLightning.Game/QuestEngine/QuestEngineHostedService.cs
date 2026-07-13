using AionLightning.Commons.Scripting;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Handlers.Templates;
using AionLightning.Game.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.QuestEngine;

/// <summary>
/// Boots the quest engine at server start: builds one template handler per data-driven
/// quest_script_data entry and registers it into <see cref="QuestEngine"/>'s npc/quest indexes,
/// then batch-compiles any hand-written quest scripts under Scripts/quest/** (see
/// <see cref="LoadHandWrittenScripts"/> for the script-authoring convention). Must run before the
/// game server starts accepting connections.
/// </summary>
public sealed class QuestEngineHostedService(
    QuestEngine engine,
    IDataManager dataManager,
    IQuestDao questDao,
    IItemDao itemDao,
    IRecipeDao recipeDao,
    ISkillDao skillDao,
    QuestRewardService rewardService,
    SpawnService spawnService,
    SkillLearnService skillLearn,
    InstanceService instanceService,
    TeleportService teleport,
    FollowService followService,
    CSharpCompilerService compiler,
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

        foreach (var data in dataManager.QuestScripts.CraftingRewards)
            engine.AddQuestHandler(new CraftingRewardsHandler(data, dataManager, questDao, rewardService, skillDao));

        foreach (var data in dataManager.QuestScripts.RelicRewards)
            engine.AddQuestHandler(new RelicRewardsHandler(data, dataManager, questDao, rewardService, itemDao));

        foreach (var data in dataManager.QuestScripts.FountainRewards)
            engine.AddQuestHandler(new FountainRewardsHandler(data, dataManager, questDao, rewardService));

        foreach (var data in dataManager.QuestScripts.SkillUse)
            engine.AddQuestHandler(new SkillUseHandler(data, dataManager, questDao, rewardService));

        foreach (var data in dataManager.QuestScripts.MentorMonsterHunt)
            engine.AddQuestHandler(new MentorMonsterHuntHandler(data, dataManager, questDao, rewardService));

        log.LogInformation(
            "QuestEngine: registered {Count} quest handler(s) ({ItemCollecting} item_collecting, {MonsterHunt} monster_hunt, " +
            "{ReportTo} report_to, {ReportToMany} report_to_many, {KillInWorld} kill_in_world, {KillSpawned} kill_spawned, " +
            "{WorkOrders} work_order, {CraftingRewards} crafting_rewards, {RelicRewards} relic_rewards, " +
            "{FountainRewards} fountain_rewards, {SkillUse} skill_use, {MentorMonsterHunt} mentor_monster_hunt)",
            engine.HandlerCount, dataManager.QuestScripts.ItemCollecting.Count,
            dataManager.QuestScripts.MonsterHunt.Count, dataManager.QuestScripts.ReportTo.Count,
            dataManager.QuestScripts.ReportToMany.Count, dataManager.QuestScripts.KillInWorld.Count,
            dataManager.QuestScripts.KillSpawned.Count, dataManager.QuestScripts.WorkOrders.Count,
            dataManager.QuestScripts.CraftingRewards.Count, dataManager.QuestScripts.RelicRewards.Count,
            dataManager.QuestScripts.FountainRewards.Count, dataManager.QuestScripts.SkillUse.Count,
            dataManager.QuestScripts.MentorMonsterHunt.Count);

        // Give hand-written scripts access to spawning + quest timers (fixed-ctor scripts can't inject).
        QuestHandlerBase.InitSpawnService(spawnService);
        QuestHandlerBase.InitEngine(engine);
        QuestHandlerBase.InitSkillLearn(skillLearn);
        QuestHandlerBase.InitInstanceServices(instanceService, teleport);
        QuestHandlerBase.InitFollowService(followService);
        LoadHandWrittenScripts();

        // Must run after every handler above (and every script handler) has registered its NPCs,
        // so the per-NPC OnQuestStart index is complete before it's joined against the spawn table.
        engine.BuildWorldQuestIndex(dataManager.Spawns, dataManager.Quests);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Batch-compiles every *.cs file under Scripts/quest/** (if the folder exists) into one
    /// assembly and registers every discovered <see cref="QuestHandlerBase"/> subclass.
    /// </summary>
    /// <remarks>
    /// <b>Script-authoring convention</b> (mirrors Java hand-written quests, which hardcode their
    /// own <c>questId</c> as a <c>private static final int</c> and pass it to
    /// <c>super(questId)</c> — no DI container in Java, just a singleton <c>QuestEngine</c>
    /// lookup): every script class must
    /// <list type="bullet">
    /// <item>subclass <see cref="QuestHandlerBase"/>,</item>
    /// <item>declare its quest id as a compile-time constant passed to the base constructor, and</item>
    /// <item>expose exactly one public constructor with the parameter list
    /// <c>(IDataManager, IQuestDao, QuestRewardService, IItemDao)</c> — the same dependencies the
    /// data-driven template handlers that need item give/remove receive (e.g.
    /// <see cref="Templates.ReportToHandler"/>), minus the XML data-row argument scripts don't
    /// have. Scripts that don't touch items simply declare and ignore the <c>IItemDao</c>
    /// parameter — every script must match this one fixed shape so the host can resolve it
    /// uniformly.</item>
    /// </list>
    /// This host resolves that fixed 4-parameter constructor by reflection and invokes it directly
    /// (no per-script DI container walk); a script needing another dependency isn't supported by
    /// this convention yet — extend the fixed parameter list here (and in the doc above) if a
    /// later batch needs one. Scripts with no matching constructor are skipped with a warning.
    /// </remarks>
    private void LoadHandWrittenScripts()
    {
        string scriptsFolder = Path.Combine(AppContext.BaseDirectory, "Scripts", "quest");
        if (!Directory.Exists(scriptsFolder))
        {
            log.LogInformation("QuestEngine: no Scripts/quest folder found, skipping hand-written script batch-compile.");
            return;
        }

        var compiled = compiler.CompileFolder(scriptsFolder, "QuestScripts");
        if (compiled is null)
        {
            log.LogWarning("QuestEngine: Scripts/quest batch-compile produced no assembly (empty folder or compile errors — see above).");
            return;
        }

        var (_, asm) = compiled.Value;
        var ctorParamTypes = new[] { typeof(IDataManager), typeof(IQuestDao), typeof(QuestRewardService), typeof(IItemDao) };
        int discovered = 0;

        foreach (var type in asm.GetExportedTypes())
        {
            if (type.IsAbstract || !type.IsAssignableTo(typeof(QuestHandlerBase))) continue;

            var ctor = type.GetConstructor(ctorParamTypes);
            if (ctor is null)
            {
                log.LogWarning("QuestEngine: script type {Type} has no ({Params}) constructor, skipping.",
                    type.FullName, string.Join(", ", ctorParamTypes.Select(t => t.Name)));
                continue;
            }

            var handler = (QuestHandlerBase)ctor.Invoke([dataManager, questDao, rewardService, itemDao]);
            engine.AddQuestHandler(handler);
            discovered++;
        }

        log.LogInformation("QuestEngine: batch-compiled {Discovered} hand-written script handler(s) from {Folder}",
            discovered, scriptsFolder);
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
