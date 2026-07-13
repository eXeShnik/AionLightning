// Port of Java data/scripts/system/handlers/quest/rider_quests/_24081SecretsWithinTheHeart.java (pralinka).
// Zone-mission sub-quest of 24080: Protector Oriata (802059, var0 0->1, hands out an evidence item),
// using that item inside the LDF4B_ItemUseArea_Q20062A zone advances var0 1->2, killing a 218767 at
// var0==2 spawns a Gravity Fault npc (702093) at the kill spot, using the Gravity Fault hands out a
// departure item and advances var0 2->3, killing a 233870 at var0==3 advances 3->4, entering the
// sensory-area zone at var0==4 flips straight to REWARD (var unchanged); turn in at Oriata.
// Note: this quest reuses the same "LDF4B_SensoryArea_Q14081_..." zone name string as the Elyos-side
// quest 14081 - kept exactly as Java wrote it (not a typo to fix here).
// Skip vs Java: (1) using the Gravity Fault no longer despawns it via the npc controller (no NPC
// despawn/controller infra in this port - matches the documented gap in
// QuestHandlerBase.UseQuestObjectAsync's `dieObject` parameter); (2) the item-use handler's temporary
// decorative npc 702314 spawn is kept, but the matching 30s-later "sweep the whole world and delete
// every npc 702314" cleanup is omitted - no World/NPC-registry access is exposed to quest handlers in
// this port. Neither omission affects quest completion.
using System;
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

public sealed class _24081SecretsWithinTheHeart : QuestHandlerBase
{
    private const int QuestIdConst = 24081;
    private const int OriataNpc       = 802059;
    private const int GravityFaultNpc = 702093;
    private const int MobA = 233870;
    private const int MobB = 218767;
    private const int EvidenceItem  = 182215406;
    private const int DepartureItem = 182215407;
    private const string SensoryZone = "LDF4B_SensoryArea_Q14081_206351_600030000";
    private const string ItemUseZone = "LDF4B_ItemUseArea_Q20062A";

    private readonly IItemDao _itemDao;

    public _24081SecretsWithinTheHeart(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(OriataNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(GravityFaultNpc).OnTalk.Add(QuestId);
        RegisterOnEnterZone(engine, SensoryZone);
        engine.RegisterQuestNpc(MobA).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(MobB).OnKill.Add(QuestId);
        engine.RegisterQuestItem(EvidenceItem, QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, 24080, isZoneMission: true, ct);

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int var0 = entry.GetVar(0);
        if (var0 == 2)
        {
            if (env.TargetId == MobB && env.Target is Npc deadNpc)
            {
                SpawnQuestNpc(600030000, player.Position.InstanceId, GravityFaultNpc,
                    deadNpc.Position.X, deadNpc.Position.Y, deadNpc.Position.Z, 0);
                return true;
            }
        }
        else if (var0 == 3)
        {
            if (env.TargetId == MobA) return await DefaultOnKillEventAsync(env, conn, MobA, 3, 4, ct);
        }
        return false;
    }

    public override async ValueTask<bool> OnEnterZoneAsync(QuestEnv env, string zoneName, GsClientConnection conn, CancellationToken ct)
    {
        if (zoneName != SensoryZone) return false;
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (entry.GetVar(0) != 4) return false;

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
        int var0        = entry.GetVar(0);
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == OriataNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var0 == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1)
                {
                    await GiveQuestItemAsync(player, conn, _itemDao, EvidenceItem, 1, ct);
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                }
                return false;
            }
            if (targetId == GravityFaultNpc)
            {
                if (dialog == DialogAction.USE_OBJECT && var0 == 2)
                {
                    await GiveQuestItemAsync(player, conn, _itemDao, DepartureItem, 1, ct); // despawn skipped, see header
                    return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
                }
                return false;
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == OriataNpc)
            {
                await RemoveQuestItemAsync(player, conn, _itemDao, DepartureItem, 1, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }
        return false;
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != EvidenceItem) return false;
        if (!player.CurrentZones.Contains(ItemUseZone)) return false;

        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(3));
            try
            {
                SpawnQuestNpc(player.Position.WorldId, player.Position.InstanceId, 702314,
                    player.Position.X, player.Position.Y, player.Position.Z, 100);
            }
            catch { }
        });

        var env = new QuestEnv(null, player, QuestId, 0);
        return await UseQuestObjectAsync(env, conn, step: 1, nextStep: 2, reward: false, varNum: 0,
            addItemId: 0, addItemCount: 0, removeItemId: itemId, removeItemCount: 1,
            movieId: 0, dieObject: false, _itemDao, ct);
    }
}
