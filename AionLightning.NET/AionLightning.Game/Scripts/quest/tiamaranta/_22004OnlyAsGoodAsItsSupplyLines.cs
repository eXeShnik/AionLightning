// Port of Java data/scripts/system/handlers/quest/tiamaranta/_22004OnlyAsGoodAsItsSupplyLines.java (Cheatkiller).
// Asmodian mirror of _12004. Accept at 205899. Talking again (SETPRO1) spawns Brax (701367) at a fixed
// spot and escorts him to fixed coords (defaultCloseDialog 0->1). Reaching advances var0 1->2
// (defaultFollowEndEvent 1,2,false); losing him (or dying/logging out at var0 1) reverts 1->0. At the
// destination talk 205948: SET_SUCCEED gives the supply crate 182212590 and flips to REWARD
// (defaultCloseDialog 2,3,true). Turn in at 205936 (USE_OBJECT shows 10002, else removes the crate + ends).
// Unblocked by the follow/escort subsystem (Java QuestService.spawnQuestNpc + newFollowingToTargetCheckTask
// -> SpawnQuestNpcAndGet + StartFollowToCoords + onNpcReachTarget/onNpcLostTarget).
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

namespace Quest.Tiamaranta;

public sealed class _22004OnlyAsGoodAsItsSupplyLines : QuestHandlerBase
{
    private const int QuestIdConst = 22004;
    private const int StartNpc  = 205899;
    private const int MidNpc    = 205948;
    private const int EndNpc    = 205936;
    private const int BraxNpc   = 701367;
    private const int CrateItem = 182212590;

    private readonly IItemDao _itemDao;

    public _22004OnlyAsGoodAsItsSupplyLines(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        RegisterOnDie(engine);
        RegisterOnLogOut(engine);
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(MidNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(EndNpc).OnTalk.Add(QuestId);
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
            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1)
                {
                    // note: Java sets Brax's walker path (walkerId "22004" + WalkManager) and plays a
                    // START_EMOTE2 emotion — cosmetic movement decoration, dropped; the escort itself is the mechanic.
                    var brax = SpawnQuestNpcAndGet(player.Position.WorldId, player.Position.InstanceId, BraxNpc, 301.4f, 1839.4f, 292.2f, (byte)8);
                    if (brax != null)
                        StartFollowToCoords(env, conn, brax, 726.15826f, 1548.5891f, 219.4855f);
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                }
            }
            else if (targetId == MidNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SET_SUCCEED)
                {
                    await GiveQuestItemAsync(player, conn, _itemDao, CrateItem, 1, ct);
                    return await DefaultCloseDialogAsync(env, conn, 2, 3, reward: true, sameNpc: false, ct);
                }
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == EndNpc)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
                await RemoveQuestItemAsync(player, conn, _itemDao, CrateItem, 1, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }
        return false;
    }

    public override async ValueTask<bool> OnDieAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (entry.GetVar(0) == 1)
        {
            entry.SetVar(0, 0);
            await UpdateQuestStatusAsync(conn, entry, ct);
            return true;
        }
        return false;
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
        }
        return false;
    }

    public override ValueTask<bool> OnNpcReachTargetAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => FollowEndAsync(env, conn, 1, 2, reward: false, ct); // Java defaultFollowEndEvent(1, 2, false)

    public override ValueTask<bool> OnNpcLostTargetAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => FollowEndAsync(env, conn, 1, 0, reward: false, ct); // Java defaultFollowEndEvent(1, 0, false)

    // Java QuestHandler.defaultFollowEndEvent + changeQuestStep: only while START and var0 == step,
    // flip to REWARD (var untouched) when reward, else roll var0 to nextStep (when it differs).
    private async ValueTask<bool> FollowEndAsync(QuestEnv env, GsClientConnection conn, int step, int nextStep, bool reward, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != step) return false;
        if (reward) await ChangeQuestStepAsync(conn, entry, -1, 0, toReward: true, ct);
        else if (nextStep != step) await ChangeQuestStepAsync(conn, entry, 0, nextStep, toReward: false, ct);
        return true;
    }
}
