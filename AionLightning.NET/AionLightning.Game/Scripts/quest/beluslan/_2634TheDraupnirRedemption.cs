// Port of Java data/scripts/system/handlers/quest/beluslan/_2634TheDraupnirRedemption.java (Cheatkiller).
// Accept at 204828. Using the cage object 700350 (USE_OBJECT at var0==0 shows 1011; SETPRO1 or a repeat
// USE_OBJECT) spawns the survivor (204830) at the player, makes it follow to 204828 and advances var0
// 0->1 (defaultCloseDialog 0,1). Reaching 204828 flips to REWARD (defaultFollowEndEvent 1,2,true);
// losing the survivor (or logging out at var0 1) rolls var0 1->0. Turn in at 204828.
// Unblocked by the follow/escort subsystem (Java QuestService.spawnQuestNpc + FOLLOW_ME +
// newFollowingToTargetCheckTask -> SpawnQuestNpcAndGet + StartFollowToNpc + onNpcReachTarget/onNpcLostTarget).
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

public sealed class _2634TheDraupnirRedemption : QuestHandlerBase
{
    private const int QuestIdConst = 2634;
    private const int StartNpc  = 204828;
    private const int CageObj   = 700350;
    private const int Survivor  = 204830;

    public _2634TheDraupnirRedemption(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService) { }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        foreach (int npc in new[] { StartNpc, CageObj, Survivor })
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
        RegisterOnLogOut(engine);
        RegisterOnReachTarget(engine);
        RegisterOnLostTarget(engine);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
        }
        else if (entry.Status == QuestStatus.START)
        {
            if (targetId == CageObj)
            {
                if (dialog == DialogAction.USE_OBJECT && entry.GetVar(0) == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                // Java switch fallthrough: USE_OBJECT(var!=0) falls into SETPRO1
                if (dialog == DialogAction.USE_OBJECT || dialog == DialogAction.SETPRO1)
                {
                    // note: Java despawns the cage object (npc.getController().onDelete()) and sends an
                    // explicit SM_NPC_INFO — cosmetic; spawn service already broadcasts the survivor's visibility.
                    var survivor = SpawnQuestNpcAndGet(player.Position.WorldId, player.Position.InstanceId, Survivor,
                        player.Position.X, player.Position.Y, player.Position.Z, (byte)0);
                    if (survivor != null)
                        StartFollowToNpc(env, conn, survivor, StartNpc);
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                }
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }
        return false;
    }

    public override async ValueTask<bool> OnNpcReachTargetAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        // Java: defaultFollowEndEvent(env, 1, 2, true) -> flip to REWARD when var0 == 1 (nextStep ignored)
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 1) return false;
        await ChangeQuestStepAsync(conn, entry, -1, 0, toReward: true, ct);
        return true;
    }

    public override async ValueTask<bool> OnNpcLostTargetAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        // Java: defaultFollowEndEvent(env, 1, 0, false) -> roll var0 1->0 when var0 == 1
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 1) return false;
        await ChangeQuestStepAsync(conn, entry, 0, 0, toReward: false, ct);
        return true;
    }

    public override async ValueTask<bool> OnLogOutAsync(QuestEnv env, GsClientConnection? conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (entry.GetVar(0) == 1)
        {
            entry.SetVar(0, 0); // Java changeQuestStep(1, 0, false)
            await QuestDao.UpsertAsync(player.ObjectId, entry, ct); // logout: persist only, no packet (conn may be null).
        }
        return false;
    }
}
