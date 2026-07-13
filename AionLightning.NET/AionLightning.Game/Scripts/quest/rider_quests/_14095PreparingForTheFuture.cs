// Port of Java data/scripts/system/handlers/quest/rider_quests/_14095PreparingForTheFuture.java (pralinka).
// Zone-mission chain (level-up gated on 10093): Lord Kaisinel (801327, var0 0->1, enters instance world
// 300330000), Oriata of the Past (802178, var0 1->2, then var0 2->3 handing out item 182215416),
// Protector Oriata (802059, var0 4->5), Ancanus (205842, var0 5->6), Fasimedes (203700, var0 6->6 flip
// to REWARD), turn in at Tirins (800527). Entering world 300330000 while var0==1 spawns Oriata of the
// Past. Using item 182215416 inside the item-use zone advances var0 3->4.
// Instance entry: Kaisinel's SETPRO1 uses the getNextAvailableInstance/registerPlayerWithInstance/
// teleportTo triad -> EnterInstanceAsync (new capability).
// Java bug fixed: Kaisinel's onDialog switch had no break after QUEST_SELECT, so talking with var0 != 0
// fell through into the SETPRO1 body and entered a fresh instance unconditionally. Ported with explicit
// dialog guards so instance entry only fires on an actual SETPRO1 dialog (same precedent as _24071).
// Skips vs Java: (1) Oriata's SETPRO3 npc.getController().onDelete() despawn dropped — no despawn API
// for quest scripts (precedent _14081). (2) The item-use branch's TeleportService2.teleportTo to
// fixed-instance world 600030000 (instanceId 1, 304/1719/295) is a plain relocation, not a
// getNextAvailableInstance entry — dropped, var transition (3->4) kept. (3) Java's registerOnLogOut is
// a no-op (no onLogOutEvent override) and is not re-registered.
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

public sealed class _14095PreparingForTheFuture : QuestHandlerBase
{
    private const int QuestIdConst      = 14095;
    private const int KaisinelNpc       = 801327;
    private const int OriataPastNpc     = 802178;
    private const int ProtectorOriataNpc = 802059;
    private const int AncanusNpc        = 205842;
    private const int FasimedesNpc      = 203700;
    private const int TirinsNpc         = 800527;
    private const int QuestItem         = 182215416;
    private const string ItemUseZone    = "IDLDF4a_ItemUseArea_Q14095";

    private readonly IItemDao _itemDao;

    public _14095PreparingForTheFuture(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterOnEnterWorld(QuestId);
        foreach (int npc in new[] { KaisinelNpc, OriataPastNpc, ProtectorOriataNpc, AncanusNpc, FasimedesNpc, TirinsNpc })
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
        engine.RegisterQuestItem(QuestItem, QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 10093, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);
        int var         = entry.GetVar(0);

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == KaisinelNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1)
                {
                    await EnterInstanceAsync(player, conn, 300330000, 224f, 251f, 125f, 10, ct);
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                }
                return false;
            }
            if (targetId == OriataPastNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.QUEST_SELECT && var == 2) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SETPRO2) return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                if (dialog == DialogAction.SETPRO3)
                {
                    await GiveQuestItemAsync(player, conn, _itemDao, QuestItem, 1, ct);
                    // note: Java npc.getController().onDelete() despawn dropped — no despawn API for quest scripts
                    return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
                }
                return false;
            }
            if (targetId == ProtectorOriataNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 4) return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.SETPRO5) return await DefaultCloseDialogAsync(env, conn, 4, 5, ct);
                return false;
            }
            if (targetId == AncanusNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 5) return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                if (dialog == DialogAction.SETPRO6) return await DefaultCloseDialogAsync(env, conn, 5, 6, ct);
                return false;
            }
            if (targetId == FasimedesNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 6) return await SendQuestDialogAsync(conn, targetObjId, 3057, ct);
                if (dialog == DialogAction.SETPRO7) return await DefaultCloseDialogAsync(env, conn, 6, 6, reward: true, sameNpc: false, ct);
                return false;
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == TirinsNpc) return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != QuestItem) return false;
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (!player.CurrentZones.Contains(ItemUseZone)) return false;

        // note: TeleportService2 relocation to fixed-instance world 600030000 (instanceId 1, 304/1719/295) dropped — plain relocation, not a getNextAvailableInstance entry; var transition kept
        var env = new QuestEnv(null, player, QuestId, 0);
        return await UseQuestObjectAsync(env, conn, 3, 4, reward: false, dieObject: false, ct);
    }

    public override ValueTask<bool> OnEnterWorldAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return ValueTask.FromResult(false);
        if (player.Position.WorldId == 300330000 && entry.GetVar(0) == 1)
            SpawnQuestNpc(300330000, player.Position.InstanceId, OriataPastNpc, 243f, 244f, 125f, 55);
        return ValueTask.FromResult(false);
    }
}
