// Port of Java data/scripts/system/handlers/quest/heiron/_1562CrossedDestiny.java (Balthazar, reworked vlog).
// Accept from Berone (204589), who hands over Berone's Necklace (182201780) at var0 0->1; take it to
// Litonos (204616) and escort him back to Berone (var0 1->2, consuming the necklace; on reach var0 2 ->
// REWARD, lost/logout rolls 2->1); turn in at Berone. Uses the follow/escort subsystem (StartFollowToNpc).
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

namespace Quest.Heiron;

public sealed class _1562CrossedDestiny : QuestHandlerBase
{
    private const int QuestIdConst = 1562;
    private const int BeroneNpc    = 204589;
    private const int LitonosNpc   = 204616;
    private const int NecklaceItem = 182201780;

    private readonly IItemDao _itemDao;

    public _1562CrossedDestiny(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(BeroneNpc).OnQuestStart.Add(QuestId);
        RegisterOnLogOut(engine);
        engine.RegisterQuestNpc(BeroneNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(LitonosNpc).OnTalk.Add(QuestId);
        RegisterOnReachTarget(engine);
        RegisterOnLostTarget(engine);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);

        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId == BeroneNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
                if (dialog == DialogAction.ASK_QUEST_ACCEPT) return await SendQuestDialogAsync(conn, targetObjId, 4, ct);
                if (dialog == DialogAction.QUEST_REFUSE_1) return await SendQuestDialogAsync(conn, targetObjId, 1004, ct);
                if (dialog == DialogAction.QUEST_ACCEPT_1)
                {
                    await StartMissionAsync(conn, player, QuestStatus.START, ct);
                    return await DefaultCloseDialogAsync(env, conn, _itemDao, 0, 1, reward: false, sameNpc: false,
                        giveItemId: NecklaceItem, giveItemCount: 1, removeItemId: 0, removeItemCount: 0, ct);
                }
            }
            return false;
        }

        if (entry.Status == QuestStatus.START && targetId == LitonosNpc)
        {
            int var = entry.GetVar(0);
            if (dialog == DialogAction.QUEST_SELECT)
            {
                if (var == 1 && (player.Inventory.FindByItemId(NecklaceItem)?.Count ?? 0) == 1)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                return await SendQuestDialogAsync(conn, targetObjId, 1438, ct);
            }
            if (dialog == DialogAction.FINISH_DIALOG)
                return await DefaultCloseDialogAsync(env, conn, 0, 0, ct);
            // Java switch fallthrough (SETPRO2 -> USE_OBJECT): both guarded by var==1, translated as guarded ifs.
            if (dialog == DialogAction.SETPRO2 && var == 1)
            {
                StartFollowToNpc(env, conn, (Npc)env.Target!, BeroneNpc); // Java step0/next0: start follow only
                return await DefaultCloseDialogAsync(env, conn, _itemDao, 1, 2, reward: false, sameNpc: false,
                    giveItemId: 0, giveItemCount: 0, removeItemId: NecklaceItem, removeItemCount: 1, ct);
            }
            if (dialog == DialogAction.USE_OBJECT && var == 1)
            {
                StartFollowToNpc(env, conn, (Npc)env.Target!, BeroneNpc);
                return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == BeroneNpc)
        {
            if (dialog == DialogAction.SELECT_QUEST_REWARD) return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }

    public override async ValueTask<bool> OnLogOutAsync(QuestEnv env, GsClientConnection? conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (entry.GetVar(0) == 2)
        {
            entry.SetVar(0, 1);
            if (conn is not null) await UpdateQuestStatusAsync(conn, entry, ct);
            else await QuestDao.UpsertAsync(player.ObjectId, entry, ct);
        }
        return false;
    }

    public override async ValueTask<bool> OnNpcReachTargetAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 2) return false;
        await ChangeQuestStepAsync(conn, entry, 0, 2, toReward: true, ct);
        return true;
    }

    public override async ValueTask<bool> OnNpcLostTargetAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 2) return false;
        await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: false, ct);
        return true;
    }
}
