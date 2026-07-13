// Port of Java data/scripts/system/handlers/quest/danaria/_13048TheChiefPriestsDaughter.java (pralinka).
// Accept at 801058. Talking the daughter (801279, SETPRO1) makes her follow the player to 206322
// (var0 0->1); reaching there flips to REWARD (defaultFollowEndEvent 1,1,true). Report to 801058.
// Losing the escort rolls var0 back 1->0. Unblocked by the follow/escort subsystem
// (Java defaultStartFollowEvent -> StartFollowToNpc + onNpcReachTarget/onNpcLostTarget).
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

namespace Quest.Danaria;

public sealed class _13048TheChiefPriestsDaughter : QuestHandlerBase
{
    private const int QuestIdConst = 13048;
    private const int StartNpc     = 801058;
    private const int FollowerNpc  = 801279;
    private const int TargetNpc    = 206322;

    public _13048TheChiefPriestsDaughter(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService) { }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(FollowerNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TargetNpc).OnTalk.Add(QuestId);
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
            if (targetId == FollowerNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (entry.GetVar(0) == 0)
                        return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                }
                else if (dialog == DialogAction.SETPRO1)
                {
                    if (env.Target is not Npc follower) return false;
                    StartFollowToNpc(env, conn, follower, TargetNpc);
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
        // Java: defaultFollowEndEvent(env, 1, 1, true) -> flip to REWARD when var0 == 1
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
            entry.SetVar(0, 0);
            await QuestDao.UpsertAsync(player.ObjectId, entry, ct); // logout: persist only, no packet (conn may be null).
            return true;
        }
        return false;
    }
}
