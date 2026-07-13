// Port of Java data/scripts/system/handlers/quest/tiamaranta/_41552EnosAvenger.java (Cheatkiller).
// Talk Enos (205968) to accept (grants Crystal Extractor 182212546). Use skill 10377 to convert the
// Extractor into a Filled Crystal (182212547), hand it in at Enos (CHECK_USER_HAS_QUEST_ITEM) which
// spawns Kalie (218781) and advances var0 0->1; killing Kalie (var0 1->2) starts a 10s timer and
// scatters 8 Crystal Shards (701144) around the corpse; loot/talk each Shard to stack up Enos'
// Fragments (182212586). The 10s timer end (or logout) flips to REWARD; turning in at Enos consumes
// the Fragments and pays a reward tier chosen by how many were collected (1 -> tier 0, 3 -> tier 1,
// 4+ -> tier 2).
// Deviation (documented, precedent tiamaranta._41526PyroMania): Java's skill conversion is gated on
// the player's current target being NPC 218497 and then despawns that NPC; OnSkillUseAsync carries
// no target information in this port, so the conversion is loosened to fire on any cast of 10377
// while the Extractor is held (and self-guarded so it converts exactly once). The onDeleteEvent-style
// despawns (Shard cleanup on turn-in/logout, target NPC removal on conversion) have no despawn API
// and are dropped - none gate progression.
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

namespace Quest.Tiamaranta;

public sealed class _41552EnosAvenger : QuestHandlerBase
{
    private const int QuestIdConst   = 41552;
    private const int EnosNpc        = 205968;
    private const int ShardNpc       = 701144;
    private const int KalieNpc       = 218781;
    private const int ConvertSkill   = 10377;
    private const int ExtractorItem  = 182212546; // Crystal Extractor (source)
    private const int FilledItem     = 182212547; // Filled Crystal (converted, turned in)
    private const int FragmentItem   = 182212586; // Enos' Fragment (reward-tier counter)

    private readonly IItemDao _itemDao;

    public _41552EnosAvenger(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterSkillUse(ConvertSkill, QuestId);
        engine.RegisterOnQuestTimerEnd(QuestId);
        engine.RegisterQuestNpc(EnosNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(EnosNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ShardNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(KalieNpc).OnKill.Add(QuestId);
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
            if (targetId == EnosNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
                if (dialog == DialogAction.QUEST_ACCEPT_SIMPLE)
                    await GiveQuestItemAsync(player, conn, _itemDao, ExtractorItem, 1, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == EnosNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                {
                    if ((player.Inventory.FindByItemId(FilledItem)?.Count ?? 0) >= 1)
                    {
                        // note: Java broadcasts a delayed SM_SYSTEM_MESSAGE (1111515) shout here - cosmetic, dropped.
                        var p = env.Target?.Position;
                        if (p is Position pos)
                            SpawnQuestNpc(pos.WorldId, pos.InstanceId, KalieNpc, 2790.97f, 1015.23f, 159.87f, 0);
                    }
                    return await CheckQuestItemsAsync(env, conn, _itemDao, 0, 1, reward: false, checkOkId: 0, checkFailId: 10001, ct);
                }
            }
            else if (targetId == ShardNpc)
            {
                // Java: ItemService.addQuestItems 182212586 x1 (stacks up), then despawn the Shard (dropped - no API).
                long current = player.Inventory.FindByItemId(FragmentItem)?.Count ?? 0;
                await GiveQuestItemAsync(player, conn, _itemDao, FragmentItem, current + 1, ct);
                return true;
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            int itemCount = (int)(player.Inventory.FindByItemId(FragmentItem)?.Count ?? 0);
            int choise = 0;
            if (itemCount > 0 && itemCount < 2) choise = 0;
            else if (itemCount > 2 && itemCount < 4) choise = 1;
            else if (itemCount >= 4) choise = 2;

            if (targetId == EnosNpc)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
                await RemoveQuestItemAsync(player, conn, _itemDao, FragmentItem, itemCount, ct);
                return await FinishQuestAsync(conn, player, choise, ct);
            }
        }
        return false;
    }

    public override async ValueTask<bool> OnLogOutAsync(QuestEnv env, GsClientConnection? conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (entry.GetVar(0) != 2) return false;

        entry.Status = QuestStatus.REWARD;
        entry.Step = 2; // Java: qs.setQuestVar(2)
        await QuestDao.UpsertAsync(player.ObjectId, entry, ct); // logout: persist only, no packet (conn may be null).
        // note: Java despawns the Crystal Shards here - no despawn API, dropped (cosmetic).
        return true;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || (entry.Status != QuestStatus.START && entry.Status != QuestStatus.REWARD)) return false;

        StartQuestTimer(env, conn, 10);
        if (env.Target?.Position is Position p)
            RndSpawn(p, ShardNpc, 8);
        return await DefaultOnKillEventAsync(env, conn, KalieNpc, 1, 2, ct);
    }

    public override async ValueTask<bool> OnQuestTimerEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || (entry.Status != QuestStatus.START && entry.Status != QuestStatus.REWARD)) return false;
        if (entry.GetVar(0) != 2) return false;

        entry.Status = QuestStatus.REWARD;
        entry.Step = 2; // Java: qs.setQuestVar(2)
        await UpdateQuestStatusAsync(conn, entry, ct);
        // note: Java despawns the Crystal Shards here - no despawn API, dropped (cosmetic).
        return true;
    }

    public override async ValueTask<bool> OnSkillUseAsync(Player player, int skillId, GsClientConnection conn, CancellationToken ct)
    {
        if (skillId != ConvertSkill) return false;
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        // Loosened target gate (see file header): convert while holding the Extractor and not yet converted.
        bool hasSource    = (player.Inventory.FindByItemId(ExtractorItem)?.Count ?? 0) >= 1;
        bool hasConverted = (player.Inventory.FindByItemId(FilledItem)?.Count ?? 0) >= 1;
        if (!hasSource || hasConverted) return false;

        await RemoveQuestItemAsync(player, conn, _itemDao, ExtractorItem, 1, ct);
        await GiveQuestItemAsync(player, conn, _itemDao, FilledItem, 1, ct);
        return true;
    }

    // Java rndSpawn: scatter `count` NPCs at a random point within radius 10 of the corpse.
    private void RndSpawn(Position origin, int npcId, int count)
    {
        for (int i = 0; i < count; i++)
        {
            float direction = Random.Shared.Next(0, 200) / 100f;
            float x1 = (float)(Math.Cos(Math.PI * direction) * 10);
            float y1 = (float)(Math.Sin(Math.PI * direction) * 10);
            SpawnQuestNpc(origin.WorldId, origin.InstanceId, npcId, origin.X + x1, origin.Y + y1, origin.Z, (byte)origin.Heading);
        }
    }
}
