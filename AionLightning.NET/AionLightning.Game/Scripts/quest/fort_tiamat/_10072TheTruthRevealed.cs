// Port of Java data/scripts/system/handlers/quest/fort_tiamat/_10072TheTruthRevealed.java (Cheatkiller).
// Elyos (world 300500000 instance): 205535 start; once 3x 182213244 collected, CHECK advances var1->2;
// SETPRO3 enters instance 300500000 (spawns 701503 + 800365, removes 182213243); talk 800365 (movie
// 495) -> talk 205579 (SET_SUCCEED). Turn in at 205535.
// Unblocked by EnterInstanceAsync (Java InstanceService.getNextAvailableInstance triad).
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.FortTiamat;

public sealed class _10072TheTruthRevealed : QuestHandlerBase
{
    private const int QuestIdConst = 10072;
    private const int InstanceWorldId = 300500000;
    private const int CollectItem = 182213244;
    private const int RemoveItem  = 182213243;

    private readonly IItemDao _itemDao;

    public _10072TheTruthRevealed(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        int[] npcs = { 800365, 205535, 205579 };
        foreach (int npc in npcs)
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterOnEnterWorld(QuestId);
    }

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 10071, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId != 205535) return false;
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        if (entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);

        if (targetId == 205535)
        {
            if (dialog == DialogAction.QUEST_SELECT)
            {
                if (var == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if ((player.Inventory.FindByItemId(CollectItem)?.Count ?? 0) >= 3)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (var == 2)
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                // Java switch fallthrough: QUEST_SELECT with no match falls into CHECK_USER_HAS_QUEST_ITEM.
                return await CheckQuestItemsAsync(env, conn, _itemDao, 1, 2, reward: false, checkOkId: 10000, checkFailId: 10001, ct);
            }
            if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                return await CheckQuestItemsAsync(env, conn, _itemDao, 1, 2, reward: false, checkOkId: 10000, checkFailId: 10001, ct);
            if (dialog == DialogAction.SETPRO1)
                return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
            if (dialog == DialogAction.SETPRO2)
                return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
            if (dialog == DialogAction.SETPRO3 && var == 2)
            {
                await EnterInstanceAsync(player, conn, InstanceWorldId, 224f, 251f, 125f, 10, ct);
                SpawnQuestNpc(InstanceWorldId, player.Position.InstanceId, 701503, 250f, 245f, 129f, 119);
                SpawnQuestNpc(InstanceWorldId, player.Position.InstanceId, 800365, 257f, 246f, 124f, 119);
                await RemoveQuestItemAsync(player, conn, _itemDao, RemoveItem, 1, ct);
                return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
            }
            return false;
        }

        if (targetId == 800365)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 3)
            {
                await PlayQuestMovieAsync(conn, player, 495, ct);
                return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
            }
            if (dialog == DialogAction.SETPRO4)
            {
                // note: Java despawns the talked-to NPC and relocates via TeleportService2 (600020000)
                //       here - no despawn/out-of-instance-teleport equivalent, dropped (cosmetic).
                return await DefaultCloseDialogAsync(env, conn, 3, 4, ct);
            }
            return false;
        }

        if (targetId == 205579)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 4)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            if (dialog == DialogAction.SET_SUCCEED)
                return await DefaultCloseDialogAsync(env, conn, 4, 5, reward: true, sameNpc: false, ct);
            return false;
        }

        return false;
    }
}
