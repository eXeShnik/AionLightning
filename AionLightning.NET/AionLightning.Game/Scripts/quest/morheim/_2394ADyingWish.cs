// Port of Java data/scripts/system/handlers/quest/morheim/_2394ADyingWish.java (Cheatkiller).
// Accept at 204343. Filling the empty container 182204130 (item use at var0==0) converts it to 182204131.
// Talk 204381 (QUEST_SELECT -> 1011; SETPRO1) spawns Orlan (790021) at the player and escorts it to the
// HALABANA_HOT_SPRINGS_220020000 zone (defaultCloseDialog 0->1). Reaching the zone flips to REWARD
// (defaultFollowEndEvent 1,1,true); losing Orlan (or dying/logging out at var0 1) rolls var0 1->0. Turn
// in at 204343 (USE_OBJECT shows 5, else ends). Unblocked by the follow/escort subsystem
// (Java QuestService.spawnQuestNpc + FOLLOW_ME + newFollowingToTargetCheckTask(ZoneName) ->
// SpawnQuestNpcAndGet + StartFollowToZone + onNpcReachTarget/onNpcLostTarget).
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

public sealed class _2394ADyingWish : QuestHandlerBase
{
    private const int QuestIdConst = 2394;
    private const int StartNpc    = 204343;
    private const int FollowerGiver = 204381;
    private const int OtherNpc    = 701147;
    private const int OrlanNpc    = 790021;
    private const int EmptyItem   = 182204130;
    private const int FilledItem  = 182204131;
    private const string TargetZone = "HALABANA_HOT_SPRINGS_220020000";

    private readonly IItemDao _itemDao;

    public _2394ADyingWish(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(FollowerGiver).OnTalk.Add(QuestId);
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
            if (targetId == FollowerGiver)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1)
                {
                    // note: Java sets Orlan's walker path (WalkManager) and plays a START_EMOTE2 emotion —
                    // cosmetic movement decoration, dropped; the escort itself is the mechanic.
                    var orlan = SpawnQuestNpcAndGet(player.Position.WorldId, player.Position.InstanceId, OrlanNpc,
                        player.Position.X, player.Position.Y, player.Position.Z, (byte)8);
                    if (orlan != null)
                        StartFollowToZone(env, conn, orlan, TargetZone);
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                }
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
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
        if (entry.GetVar(0) == 1)
        {
            entry.SetVar(0, 0); // Java setQuestVar(0)
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
            entry.SetVar(0, 0); // Java setQuestVar(0)
            await QuestDao.UpsertAsync(player.ObjectId, entry, ct); // logout: persist only, no packet (conn may be null).
        }
        return false;
    }

    public override async ValueTask<bool> OnNpcReachTargetAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        // Java: defaultFollowEndEvent(env, 1, 1, true) -> flip to REWARD when var0 == 1 (nextStep ignored)
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
}
