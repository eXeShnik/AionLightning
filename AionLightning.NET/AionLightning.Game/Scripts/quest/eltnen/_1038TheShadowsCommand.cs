// Port of Java data/scripts/system/handlers/quest/eltnen/_1038TheShadowsCommand.java (Rhys2002).
// Zone-mission quest, part of the Kaidan Fortress chain (1300): use the Underground Temple Artifact
// (700162, var 0->1, movie 34); Actaeon (203933) advances var 1->2->3, requires looting Philipemos's
// Corpse (700172, drops item 182201007 at var 2, var 2->3 via OnItemGetAsync), then hands in three
// collected fragments plus the corpse item at var 3->4 and checks a fourth quest-item set at var
// 4->5; Dionera (203991) plays movie 35 at var 6->7, which spawns a Kaidan guard (204005) with a
// 180s timer - kill it to flip to REWARD (or the timer reverts var 7->6); turn in at Dionera.
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.Eltnen;

public sealed class _1038TheShadowsCommand : QuestHandlerBase
{
    private const int QuestIdConst = 1038;
    private const int ActaeonNpc   = 203933;
    private const int CorpseObj    = 700172;
    private const int DioneraNpc   = 203991;
    private const int ArtifactObj  = 700162;
    private const int GuardNpc     = 204005;

    private const int CorpseItem   = 182201007;
    private const int MaterialItem1 = 182201015;
    private const int MaterialItem2 = 182201016;
    private const int MaterialItem3 = 182201017;

    private readonly IItemDao _itemDao;

    public _1038TheShadowsCommand(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterOnQuestTimerEnd(QuestId);
        engine.RegisterOnQuestMovieEnd(35, QuestId);
        engine.RegisterQuestNpc(GuardNpc).OnKill.Add(QuestId);
        RegisterQuestDrop(engine, CorpseObj, CorpseItem, 1, 100, 2);
        engine.RegisterItemGet(CorpseItem, QuestId);
        foreach (int npc in new[] { ActaeonNpc, CorpseObj, DioneraNpc, ArtifactObj })
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 1300, isZoneMission: true, ct);

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => await DefaultOnKillEventAsync(env, conn, GuardNpc, 7, reward: true, ct);

    public override async ValueTask<bool> OnItemGetAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != CorpseItem) return false;
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 2) return false;

        await ChangeQuestStepAsync(conn, entry, 0, 3, toReward: false, ct);
        return true;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);

            if (targetId == ArtifactObj)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await UseQuestObjectAsync(env, conn, 0, 1, false, 0, 0, 0, 0, 0, 34, false, null, ct);
                return false;
            }

            if (targetId == ActaeonNpc)
            {
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT when var == 1:
                        return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    case DialogAction.QUEST_SELECT when var == 3:
                        return await SendQuestDialogAsync(conn, targetObjId, 1694, ct);
                    case DialogAction.QUEST_SELECT when var == 4:
                        return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                    case DialogAction.QUEST_SELECT when var == 5:
                        return await SendQuestDialogAsync(conn, targetObjId, 2035, ct);
                    case DialogAction.CHECK_USER_HAS_QUEST_ITEM:
                        return await CheckQuestItemsAsync(env, conn, _itemDao, 4, 5, reward: false, checkOkId: 2035, checkFailId: 2120, ct);
                    case DialogAction.SETPRO2:
                        return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                    case DialogAction.SETPRO3:
                        await RemoveQuestItemAsync(player, conn, _itemDao, MaterialItem1, 1, ct);
                        await RemoveQuestItemAsync(player, conn, _itemDao, MaterialItem2, 1, ct);
                        await RemoveQuestItemAsync(player, conn, _itemDao, MaterialItem3, 1, ct);
                        return await DefaultCloseDialogAsync(env, conn, _itemDao, 3, 4, reward: false, sameNpc: false,
                            giveItemId: 0, giveItemCount: 0, removeItemId: CorpseItem, removeItemCount: 1, ct);
                    case DialogAction.SETPRO4:
                        return await DefaultCloseDialogAsync(env, conn, 5, 6, ct);
                    case DialogAction.FINISH_DIALOG:
                        return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                    default:
                        return false;
                }
            }

            if (targetId == CorpseObj)
                return var == 2 && dialog == DialogAction.USE_OBJECT;

            if (targetId == DioneraNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 6)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.SETPRO5)
                {
                    await PlayQuestMovieAsync(conn, player, 35, ct);
                    return await DefaultCloseDialogAsync(env, conn, 6, 7, ct);
                }
                return false;
            }

            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == DioneraNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }

    public override ValueTask<bool> OnMovieEndAsync(QuestEnv env, int movieId, GsClientConnection conn, CancellationToken ct)
    {
        if (movieId != 35) return ValueTask.FromResult(false);

        StartQuestTimer(env, conn, 180);
        SpawnQuestNpc(210020000, 1, GuardNpc, 1768.16f, 924.47f, 422.02f, 0);
        return ValueTask.FromResult(true);
    }

    public override async ValueTask<bool> OnQuestTimerEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 7) return false;

        await ChangeQuestStepAsync(conn, entry, 0, 6, toReward: false, ct);
        return true;
    }
}
