// Port of Java data/scripts/system/handlers/quest/rider_quests/_14081TheHeartOfThePresent.java (pralinka).
// Zone-mission sub-quest of 14080: talk to Protector Oriata (802059, var0 0->1, gives item
// 182215404), kill mob 233869 while var0==2 (var0 2->3), killing mob 218766 while var0==3 spawns the
// Exploding Rock object (702090) at its death spot, using it (var0 3->4), entering the sensory zone
// at var0==4 flips straight to REWARD, turn in at Protector Oriata. Using item 182215404 inside the
// item-use zone spawns a temporary npc after 3s and finishes at var0 1->2.
// Java bug fixed: onDialogEvent's switch on Protector Oriata (802059) had no break after the
// QUEST_SELECT case, so talking with var0 != 0 fell through into the SETPRO1 body and granted a
// duplicate copy of item 182215404 before defaultCloseDialog's var==0 guard failed. Fixed here so
// the item-give only fires on an actual SETPRO1 dialog.
// Skip vs Java: the Exploding Rock's USE_OBJECT dialog and the item-use zone case both delete their
// triggering npc (Java npc.getController().onDelete()) - no despawn API is exposed to hand-written
// quest scripts in this port (same simplification as sarpan._11512LoversLost), so those npcs are
// simply left in place; likewise the item-use zone's 30s "despawn every npc 702314 in the world"
// scavenger (Java World.getInstance().getNpcs()) has no equivalent and is dropped, keeping only the
// 3s delayed spawn.
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

namespace Quest.RiderQuests;

public sealed class _14081TheHeartOfThePresent : QuestHandlerBase
{
    private const int QuestIdConst   = 14081;
    private const int OriataNpc      = 802059;
    private const int ExplodingRockNpc = 702090;
    private const int RockSourceMob  = 218766;
    private const int KillMobNpc     = 233869;
    private const int TempSpawnNpc   = 702314;
    private const int EvidenceItem   = 182215404;
    private const string SensoryAreaZone  = "LDF4B_SensoryArea_Q14081_206351_600030000";
    private const string ItemUseAreaZone  = "LDF4B_ITEMUSEAREA_Q10060A";

    private readonly IItemDao _itemDao;

    public _14081TheHeartOfThePresent(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(OriataNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ExplodingRockNpc).OnTalk.Add(QuestId);
        RegisterOnEnterZone(engine, SensoryAreaZone);
        engine.RegisterQuestNpc(KillMobNpc).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(RockSourceMob).OnKill.Add(QuestId);
        engine.RegisterQuestItem(EvidenceItem, QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 14080, isZoneMission: true, ct);

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);
        if (var == 3)
        {
            if (env.TargetId == RockSourceMob && env.Target is not null)
            {
                var pos = env.Target.Position;
                SpawnQuestNpc(pos.WorldId, pos.InstanceId, ExplodingRockNpc, pos.X, pos.Y, pos.Z, 0);
                return true;
            }
        }
        else if (var == 2)
        {
            if (env.TargetId == KillMobNpc)
                return await DefaultOnKillEventAsync(env, conn, KillMobNpc, 2, 3, ct);
        }
        return false;
    }

    public override async ValueTask<bool> OnEnterZoneAsync(QuestEnv env, string zoneName, GsClientConnection conn, CancellationToken ct)
    {
        if (zoneName != SensoryAreaZone) return false;

        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 4) return false;

        await ChangeQuestStepAsync(conn, entry, 0, 4, toReward: true, ct);
        return true;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);
        int var         = entry.GetVar(0);

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == OriataNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return var == 0 && await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1)
                {
                    if (var != 0) return false;
                    await GiveQuestItemAsync(player, conn, _itemDao, EvidenceItem, 1, ct);
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                }
                return false;
            }
            if (targetId == ExplodingRockNpc)
            {
                if (dialog == DialogAction.USE_OBJECT && var == 3)
                    return await DefaultCloseDialogAsync(env, conn, 3, 4, ct);
                return false;
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == OriataNpc) return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != EvidenceItem) return false;
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (!player.CurrentZones.Contains(ItemUseAreaZone)) return false;

        var pos = player.Position;
        _ = Task.Run(async () =>
        {
            await Task.Delay(3000, CancellationToken.None);
            SpawnQuestNpc(pos.WorldId, pos.InstanceId, TempSpawnNpc, pos.X, pos.Y, pos.Z, 100);
        });

        var env = new QuestEnv(null, player, QuestId, 0);
        return await UseQuestObjectAsync(env, conn, 1, 2, reward: false, dieObject: false, ct);
    }
}
