// Port of Java data/scripts/system/handlers/quest/theobomos/_3050RescuingRuria.java (Balthazar/vlog).
// Get the antidote (182208035), bring it to Ruria (798211), then escort her to Melleas (798208):
// talking Ruria consumes the antidote, plays movie 370 and starts the follow (var0 0->1); reaching
// Melleas advances var0 1->2 (defaultFollowEndEvent 1,2,false). Talking Melleas (SET_SUCCEED at
// var0==2) flips to REWARD. Report to Rosina (798190). Losing the escort rolls var0 back 1->0.
// Unblocked by the follow/escort subsystem (Java defaultStartFollowEvent -> StartFollowToNpc +
// onNpcReachTarget/onNpcLostTarget).
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

namespace Quest.Theobomos;

public sealed class _3050RescuingRuria : QuestHandlerBase
{
    private const int QuestIdConst  = 3050;
    private const int RuriaNpc      = 798211;
    private const int MelleasNpc    = 798208;
    private const int RosinaNpc     = 798190;
    private const int AntidoteItem  = 182208035;

    private readonly IItemDao _itemDao;

    public _3050RescuingRuria(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(RuriaNpc).OnQuestStart.Add(QuestId);
        RegisterOnLogOut(engine);
        engine.RegisterQuestNpc(RuriaNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(MelleasNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(RosinaNpc).OnTalk.Add(QuestId);
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
            if (targetId == RuriaNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == RuriaNpc)
            {
                if (env.Target is not Npc follower) return false;
                int var = entry.GetVar(0);

                // QUEST_SELECT at var0==0: offer to escort (1011 if antidote in bag, else 1097).
                if (dialog == DialogAction.QUEST_SELECT && var == 0)
                {
                    long itemCount = player.Inventory.FindByItemId(AntidoteItem)?.Count ?? 0;
                    return await SendQuestDialogAsync(conn, targetObjId, itemCount >= 1 ? 1011 : 1097, ct);
                }

                // USE_OBJECT at var0==0: start the escort immediately (Java USE_OBJECT case, var==0 branch).
                if (dialog == DialogAction.USE_OBJECT && var == 0)
                {
                    StartFollowToNpc(env, conn, follower, MelleasNpc);
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                }

                // Java switch fallthrough: SELECT_ACTION_1012 (and QUEST_SELECT/USE_OBJECT at var!=0)
                // consume the antidote before falling into SETPRO1; SETPRO1 itself only plays the movie.
                bool consume = dialog == DialogAction.SELECT_ACTION_1012
                    || ((dialog == DialogAction.QUEST_SELECT || dialog == DialogAction.USE_OBJECT) && var != 0);
                bool escort = consume || dialog == DialogAction.SETPRO1;
                if (escort)
                {
                    if (consume)
                        await RemoveQuestItemAsync(player, conn, _itemDao, AntidoteItem, 1, ct);
                    await PlayQuestMovieAsync(conn, player, 370, ct);
                    StartFollowToNpc(env, conn, follower, MelleasNpc);
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                }
                return false;
            }

            if (targetId == MelleasNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && entry.GetVar(0) == 2)
                    return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                // Java switch fallthrough: QUEST_SELECT(var!=2) falls into SET_SUCCEED (self-guards var==2).
                if (dialog == DialogAction.SET_SUCCEED || dialog == DialogAction.QUEST_SELECT)
                    return await DefaultCloseDialogAsync(env, conn, 2, 2, reward: true, sameNpc: false, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == RosinaNpc)
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
        // Java: defaultFollowEndEvent(env, 1, 2, false) -> advance var0 1->2 when var0 == 1
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 1) return false;
        await ChangeQuestStepAsync(conn, entry, 0, 2, toReward: false, ct);
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
