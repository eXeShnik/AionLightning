// Port of Java data/scripts/system/handlers/quest/gelkmaros/_21105CoweringRefugee.java (Cheatkiller).
// Talk to 799276, gives item 182207857 on accept; talk to 700812 (spawns either 799366 or 216086
// at its position, chosen at random, via the Batch 0.2 SpawnQuestNpc primitive); talk to the
// spawned 799366 (SET_SUCCEED removes the item, var 0->1, reward); turn in at 799276.
// Skip vs Java: qs.canRepeat() (repeatable-quest gate) is approximated as "no active entry" like
// the rest of this port (see QuestEngine.ComputeNearbyQuests); the source npc's
// scheduleRespawn()/onDelete() calls are skipped (no Npc AI/controller subsystem in this port yet)
// — the underlying var transitions still work, this only drops the despawn/respawn visual.
using System;
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

namespace Quest.Gelkmaros;

public sealed class _21105CoweringRefugee : QuestHandlerBase
{
    private const int QuestIdConst  = 21105;
    private const int StartNpc      = 799276;
    private const int TriggerNpc    = 700812;
    private const int RefugeeNpc    = 799366;
    private const int AltRefugeeNpc = 216086;
    private const int ItemId        = 182207857;

    private readonly IItemDao _itemDao;

    public _21105CoweringRefugee(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TriggerNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(RefugeeNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null)
        {
            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
                if (env.DialogId == (int)DialogAction.QUEST_ACCEPT_1)
                {
                    if (await GiveQuestItemAsync(player, conn, _itemDao, ItemId, 1, ct))
                        return await SendQuestStartDialogAsync(env, conn, ct);
                    return true;
                }
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == RefugeeNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SET_SUCCEED)
                {
                    await RemoveQuestItemAsync(player, conn, _itemDao, ItemId, 1, ct);
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, reward: true, sameNpc: false, ct);
                }
                return false;
            }

            if (targetId == TriggerNpc && env.Target is not null)
            {
                var pos = env.Target.Position;
                int spawnNpcId = Random.Shared.Next(0, 2) == 0 ? RefugeeNpc : AltRefugeeNpc;
                SpawnQuestNpc(pos.WorldId, pos.InstanceId, spawnNpcId, pos.X, pos.Y, pos.Z, (byte)pos.Heading);
                return true;
            }
        }

        if (entry.Status == QuestStatus.REWARD && targetId == StartNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
