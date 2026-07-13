// Port of Java data/scripts/system/handlers/quest/tiamaranta_eye/_12031APathUncertain.java (Cheatkiller).
// Start at 205957 (grants item 182212594 on accept). Using that item spawns a trail of 701416
// markers plus NPC 206233 and advances var 0->1; walking within range of 206233 (onAtDistance)
// flips the quest to reward.
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

namespace Quest.TiamarantaEye;

public sealed class _12031APathUncertain : QuestHandlerBase
{
    private const int QuestIdConst = 12031;
    private const int StartNpc     = 205957;
    private const int TrailMarker  = 701416;
    private const int AtDistNpc    = 206233;
    private const int QuestItemId  = 182212594;

    private readonly IItemDao _itemDao;

    public _12031APathUncertain(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestItem(QuestItemId, QuestId);
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        RegisterOnAtDistance(engine, AtDistNpc);
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
            if (targetId != StartNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            // Java sendQuestStartDialog(env, 182212594, 1): give the item as part of accept.
            if (env.DialogId == (int)DialogAction.QUEST_ACCEPT_1)
            {
                if (!await GiveQuestItemAsync(player, conn, _itemDao, QuestItemId, 1, ct)) return true;
            }
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.REWARD && targetId == StartNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }

    // Java onItemUseEvent: while START, spawn the marker trail + AtDistNpc, then useQuestItem(0,1,false).
    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != QuestItemId) return false;
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int worldId    = player.Position.WorldId;
        int instanceId = player.Position.InstanceId;
        // note: Java addNewSpawn's trailing 2-minute despawn timer is dropped (SpawnQuestNpc has no timed despawn).
        SpawnQuestNpc(worldId, instanceId, TrailMarker, 217.481f, 631.22f, 1183.85f, 0);
        SpawnQuestNpc(worldId, instanceId, TrailMarker, 216.999f, 643.04f, 1183.85f, 0);
        SpawnQuestNpc(worldId, instanceId, TrailMarker, 216.1699f, 681.79f, 1192f, 0);
        SpawnQuestNpc(worldId, instanceId, TrailMarker, 213.949f, 743.243f, 1200.16f, 0);
        SpawnQuestNpc(worldId, instanceId, TrailMarker, 122.913f, 766.99f, 1205.95f, 0);
        SpawnQuestNpc(worldId, instanceId, TrailMarker, 171.45f, 767.19f, 1200.464f, 0);
        SpawnQuestNpc(worldId, instanceId, TrailMarker, 140.43f, 770.06f, 1204.17f, 0);
        SpawnQuestNpc(worldId, instanceId, AtDistNpc, 123.87f, 768.8279f, 1205.9581f, 0);

        // Java useQuestItem(env, item, 0, 1, false): guard var 0 == 0, consume item, advance to 1.
        if (entry.GetVar(0) != 0) return false;
        await RemoveQuestItemAsync(player, conn, _itemDao, QuestItemId, 1, ct);
        await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: false, ct);
        return true;
    }

    public override async ValueTask<bool> OnAtDistanceAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        // Java changeQuestStep(env, 1, 1, true): reward flip guarded on var 0 == 1.
        if (entry.GetVar(0) == 1)
            await ChangeQuestStepAsync(conn, entry, varIdx: -1, newValue: 1, toReward: true, ct);
        return true;
    }
}
