// Port of Java data/scripts/system/handlers/quest/sarpan/_41300TheRedStallion.java (Cheatkiller).
// Accept at 205763 (grants the mount item 190100014 on accept). Riding that mount advances var0 0->1
// (OnRide hook). Turn in at 205794.
// note: the OnRide hook is wired but never fires yet — the mount/ride system is not ported (see
// QuestEngine.RegisterOnRide) — so RideActionAsync below is migrated-but-unreachable. Without a mount
// system the player currently cannot reach var0==1, but the structure is preserved 1:1 with Java.
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

namespace Quest.Sarpan;

public sealed class _41300TheRedStallion : QuestHandlerBase
{
    private const int QuestIdConst = 41300;
    private const int StartNpc  = 205763;
    private const int TurnInNpc = 205794;
    private const int RideItem  = 190100014;

    private readonly IItemDao _itemDao;

    public _41300TheRedStallion(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
        RegisterOnRide(engine);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                // Java: sendQuestStartDialog(env, 190100014, 1) — start and grant the mount item.
                if (env.DialogId == (int)DialogAction.QUEST_ACCEPT_1)
                {
                    if (await GiveQuestItemAsync(player, conn, _itemDao, RideItem, 1, ct))
                        return await SendQuestStartDialogAsync(env, conn, ct);
                    return true;
                }
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
        }
        else if (entry.Status == QuestStatus.START)
        {
            if (targetId == TurnInNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (entry.GetVar(0) == 1)
                        return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                }
                else if (dialog == DialogAction.SELECT_QUEST_REWARD)
                {
                    return await DefaultCloseDialogAsync(env, conn, 1, 1, reward: true, sameNpc: true, ct);
                }
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == TurnInNpc)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }
        return false;
    }

    // Java rideAction(env, rideItemId): riding the granted mount advances the quest.
    // note: dispatch passes the ride item id as npcId; never fires until the mount system is ported.
    public override async ValueTask<bool> OnRideAsync(QuestEnv env, int npcId, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is not null && entry.Status == QuestStatus.START)
        {
            if (npcId == RideItem)
            {
                await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: false, ct);
                return true;
            }
        }
        return false;
    }
}
