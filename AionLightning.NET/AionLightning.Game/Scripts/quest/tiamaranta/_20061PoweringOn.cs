// Port of Java data/scripts/system/handlers/quest/tiamaranta/_20061PoweringOn.java (zhkchi).
// Auto-starts on world entry (onEnterWorldEvent). Talk to Adella (205886) to advance var 0->1
// (SETPRO1); entering GRAVITY_WELL flips 1->2; kill 218767 at var 2 to flip 2->3 (also respawns
// 701233 each time any kill lands while var==2, matching Java's own unconditional respawn call);
// interacting with 701233 at var 3 grants item 182212558 and despawns it; turn in at Garnon
// (800018), which also removes the item on the USE_OBJECT branch.
// Java bug: onKillEvent's guard is `qs.getStatus() != START && qs.getQuestVarById(0) != 2` (an AND
// of negatives that can never gate correctly - kept as-is as `entry.Status != START || GetVar(0) !=
// 2` would actually change behavior, so this mirrors Java's literal condition instead).
// Skip vs Java: interacting with 701233 calls env.getVisibleObject().getController().onDelete() to
// despawn it - no despawn-by-id API is ported (see migration_plan.md); the item grant and dialog
// return are kept, only the despawn is dropped (cosmetic - a stale prop stays behind).
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

namespace Quest.Tiamaranta;

public sealed class _20061PoweringOn : QuestHandlerBase
{
    private const int QuestIdConst = 20061;
    private const int AdellaNpc    = 205886;
    private const int GarnonNpc    = 800018;
    private const int KillNpc      = 218767;
    private const int ObjectNpc    = 701233;
    private const int CoreItemId   = 182212558;
    private const string GravityWellZone = "GRAVITY_WELL_600030000";

    private readonly IItemDao _itemDao;

    public _20061PoweringOn(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(AdellaNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(AdellaNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(GarnonNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(KillNpc).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(ObjectNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestItem(CoreItemId, QuestId);
        RegisterOnEnterZone(engine, GravityWellZone);
        engine.RegisterOnLevelUp(QuestId);
    }

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, 20060, isZoneMission: true, ct);

    public override async ValueTask<bool> OnEnterWorldAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        if (env.Player.Quests.Get(QuestId) is not null) return false;
        return await StartMissionAsync(conn, env.Player, QuestStatus.START, ct);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == AdellaNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SELECT_ACTION_1012)
                    return await SendQuestDialogAsync(conn, targetObjId, 1012, ct);
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
            }
            else if (targetId == ObjectNpc && entry.GetVar(0) == 3)
            {
                await GiveQuestItemAsync(player, conn, _itemDao, CoreItemId, 1, ct);
                return true;
            }
        }
        else if (entry.Status == QuestStatus.REWARD && targetId == GarnonNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
            {
                await RemoveQuestItemAsync(player, conn, _itemDao, CoreItemId, 1, ct);
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            }
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }

    public override async ValueTask<bool> OnEnterZoneAsync(QuestEnv env, string zoneName, GsClientConnection conn, CancellationToken ct)
    {
        if (zoneName != GravityWellZone) return false;
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 1) return false;
        await ChangeQuestStepAsync(conn, entry, 0, 2, toReward: false, ct);
        return true;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START && entry.GetVar(0) != 2) return false;

        if (env.TargetId == KillNpc)
            await ChangeQuestStepAsync(conn, entry, 0, 3, toReward: false, ct);

        SpawnQuestNpc(600030000, player.Position.InstanceId, ObjectNpc, 1712.9526f, 514.41455f, 200.16928f, 76);
        return false;
    }
}
