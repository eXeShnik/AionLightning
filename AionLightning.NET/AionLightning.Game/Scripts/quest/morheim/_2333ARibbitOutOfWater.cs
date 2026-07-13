// Port of Java data/scripts/system/handlers/quest/morheim/_2333ARibbitOutOfWater.java (Cheatkiller).
// Accept at 798084. Talk it (QUEST_SELECT var0==0 -> 1011, var0==1 -> 1352). Filling the empty container
// 182204130 (item use at var0==0) converts it to 182204131. CHECK_USER_HAS_QUEST_ITEM hands the filled
// container in (checkQuestItems 0->1). SETPRO2 spawns Debrie (204416) at the player and escorts it to the
// DF2_ITEMUSEAREA_Q2333 zone (defaultCloseDialog 1->2). Reaching the zone flips to REWARD
// (defaultFollowEndEvent 2,2,true); losing Debrie (or dying/logging out at var0 2) rolls var0 2->1.
// Turn in at 798084. Unblocked by the follow/escort subsystem (Java QuestService.spawnQuestNpc + FOLLOW_ME
// + newFollowingToTargetCheckTask(ZoneName) -> SpawnQuestNpcAndGet + StartFollowToZone + onNpcReachTarget/onNpcLostTarget).
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

namespace Quest.Morheim;

public sealed class _2333ARibbitOutOfWater : QuestHandlerBase
{
    private const int QuestIdConst = 2333;
    private const int StartNpc    = 798084;
    private const int OtherNpc    = 701147;
    private const int DebrieNpc   = 204416;
    private const int EmptyItem   = 182204130;
    private const int FilledItem  = 182204131;
    private const string TargetZone = "DF2_ITEMUSEAREA_Q2333";

    private readonly IItemDao _itemDao;

    public _2333ARibbitOutOfWater(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(OtherNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestItem(EmptyItem, QuestId);
        RegisterOnLostTarget(engine);
        RegisterOnReachTarget(engine);
        RegisterOnDie(engine);
        RegisterOnLogOut(engine);
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
            int var = entry.GetVar(0);
            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                    if (var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                }
                else if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                {
                    return await CheckQuestItemsAsync(env, conn, _itemDao, 0, 1, false, 10000, 10001, ct);
                }
                else if (dialog == DialogAction.SETPRO2)
                {
                    // note: Java sets Debrie's walker path (WalkManager) and plays a START_EMOTE2 emotion —
                    // cosmetic movement decoration, dropped; the escort itself is the mechanic.
                    var debrie = SpawnQuestNpcAndGet(player.Position.WorldId, player.Position.InstanceId, DebrieNpc,
                        player.Position.X, player.Position.Y, player.Position.Z, (byte)8);
                    if (debrie != null)
                        StartFollowToZone(env, conn, debrie, TargetZone);
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                }
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == StartNpc)
                return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        // Java: useQuestItem(env, item, 0, 0, false, 182204131, 1, 0, 0) — at var0==0, consume the empty
        // container and give the filled one; the 0->0 step change is a no-op.
        if (itemId != EmptyItem) return false;
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 0) return false;
        // note: Java plays a 3s SM_ITEM_USAGE_ANIMATION before applying — cosmetic, dropped.
        await RemoveQuestItemAsync(player, conn, _itemDao, EmptyItem, 1, ct);
        await GiveQuestItemAsync(player, conn, _itemDao, FilledItem, 1, ct);
        return true;
    }

    public override async ValueTask<bool> OnDieAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (entry.GetVar(0) == 2)
        {
            entry.SetVar(0, 1); // Java setQuestVar(1)
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
        if (entry.GetVar(0) == 2)
        {
            entry.SetVar(0, 1); // Java setQuestVar(1)
            await QuestDao.UpsertAsync(player.ObjectId, entry, ct); // logout: persist only, no packet (conn may be null).
        }
        return false;
    }

    public override async ValueTask<bool> OnNpcReachTargetAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        // Java: defaultFollowEndEvent(env, 2, 2, true) -> flip to REWARD when var0 == 2 (nextStep ignored)
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 2) return false;
        await ChangeQuestStepAsync(conn, entry, -1, 0, toReward: true, ct);
        return true;
    }

    public override async ValueTask<bool> OnNpcLostTargetAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        // Java: defaultFollowEndEvent(env, 2, 1, false) -> roll var0 2->1 when var0 == 2
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 2) return false;
        await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: false, ct);
        return true;
    }
}
