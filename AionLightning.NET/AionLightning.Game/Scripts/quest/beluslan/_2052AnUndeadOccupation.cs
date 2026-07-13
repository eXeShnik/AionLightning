// Port of Java data/scripts/system/handlers/quest/beluslan/_2052AnUndeadOccupation.java.
// Zone-mission quest: 204715 (var0->1), 204801 (var1->2, var7->8), kill 213044/213045 across
// [2,8), 204805 (collect-item check gives 182204304 and bumps var, var8->9), then using the
// gathered item (182204304) at var==10 plays movie 234 and flips to REWARD.
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

namespace Quest.Beluslan;

public sealed class _2052AnUndeadOccupation : QuestHandlerBase
{
    private const int QuestIdConst = 2052;
    private const int Npc715 = 204715;
    private const int Npc801 = 204801;
    private const int Npc805 = 204805;
    private const int CollectItem = 182204304;
    private static readonly int[] _mobIds = [213044, 213045];

    private readonly IItemDao _itemDao;

    public _2052AnUndeadOccupation(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestItem(CollectItem, QuestId);
        engine.RegisterQuestNpc(213044).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(Npc715).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Npc801).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Npc805).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 2500, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null) return false;

        int var = entry.GetVar(0);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == Npc715)
            {
                if (dialog == DialogAction.USE_OBJECT) return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
                if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD) return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
            return false;
        }
        if (entry.Status != QuestStatus.START) return false;

        if (targetId == Npc715)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog == DialogAction.SETPRO1) return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
            return false;
        }
        if (targetId == Npc801)
        {
            if (dialog == DialogAction.QUEST_SELECT)
            {
                if (var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (var == 7) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                return false;
            }
            if (dialog == DialogAction.SETPRO2) return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
            if (dialog == DialogAction.SETPRO3) return await DefaultCloseDialogAsync(env, conn, 7, 8, ct);
            return false;
        }
        if (targetId == Npc805)
        {
            if (dialog == DialogAction.QUEST_SELECT)
            {
                if (var == 8) return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                if (var == 9) return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                return false;
            }
            if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                return await CheckQuestItemsAsync(env, conn, _itemDao, 8, 9, false, 10000, 10001, CollectItem, 1, ct);
            if (dialog == DialogAction.SETPRO4) return await DefaultCloseDialogAsync(env, conn, 8, 9, ct);
            return false;
        }
        return false;
    }

    public override ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnKillEventAsync(env, conn, _mobIds, 2, 8, ct);

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != CollectItem) return false;
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.GetVar(0) != 10) return false;

        await RemoveQuestItemAsync(player, conn, _itemDao, CollectItem, 1, ct);
        await PlayQuestMovieAsync(conn, player, 234, ct);
        entry.Status = QuestStatus.REWARD;
        await UpdateQuestStatusAsync(conn, entry, ct);
        return true;
    }
}
