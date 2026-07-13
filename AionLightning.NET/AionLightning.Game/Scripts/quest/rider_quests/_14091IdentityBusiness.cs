// Port of Java data/scripts/system/handlers/quest/rider_quests/_14091IdentityBusiness.java (pralinka).
// Zone-mission sub-quest of 14090: talk to Oriata of the Past (802061, var0 0->1, spawns Kaisinel
// 802063), Kaisinel (802063, var0 1->2, gives item 182215411), Agent Outremus (798926, var0 2->3),
// Eremitia (798600, var0 3->4, then 5->6), Fasimedes (203700, var0 4->5), Garnon (205987, var0
// 6->reward), turn in at Crispin (800165). onEnterWorldEvent (re)spawns Oriata of the Past on
// entering world 300500000 while at var0==0.
// Java bugs fixed: onDialogEvent's switches on Oriata of the Past (802061) and Kaisinel (802063) had
// no break after their QUEST_SELECT cases, so talking with var0 outside their handled values fell
// through into the SETPRO1/SETPRO2 bodies and (a) respawned a duplicate Kaisinel and (b) granted a
// duplicate copy of item 182215411, both before defaultCloseDialog's var guard failed. Fixed here so
// those side effects only fire on their own actual dialog id.
// Skip vs Java: this quest's own progression only uses a static-instance TeleportService2.teleportTo
// call (Kaisinel's SETPRO2) and npc.getController().onDelete() calls (Oriata of the Past's SETPRO1,
// Kaisinel's SETPRO2) - neither TeleportService2 nor a despawn API exist in this port, so those are
// dropped, keeping the var transitions and item grants (same precedent as
// quest/rider_quests/_14024AKrallingSuspicion.cs and sarpan._11512LoversLost). onLogOutEvent (would
// reset var0 back to 0 on disconnect while var0 <= 2) has no OnLogOut hook in this port and is
// omitted (same simplification as inggison._10024WillTheAetherRain.cs). Reaching this quest normally
// requires 14090 (_14090TracesOfThePast), which is skipped in this port (needs InstanceService) - this
// handler is fully functional but currently unreachable via the level-up path.
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

namespace Quest.RiderQuests;

public sealed class _14091IdentityBusiness : QuestHandlerBase
{
    private const int QuestIdConst   = 14091;
    private const int OriataPastNpc  = 802061;
    private const int KaisinelNpc    = 802063;
    private const int OutremusNpc    = 798926;
    private const int EremitiaNpc    = 798600;
    private const int FasimedesNpc   = 203700;
    private const int GarnonNpc      = 205987;
    private const int CrispinNpc     = 800165;
    private const int KaisinelItem   = 182215411;
    private const int InstanceWorldId = 300500000;

    private readonly IItemDao _itemDao;

    public _14091IdentityBusiness(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterOnEnterWorld(QuestId);
        foreach (int npc in new[] { OriataPastNpc, KaisinelNpc, OutremusNpc, EremitiaNpc, FasimedesNpc, GarnonNpc, CrispinNpc })
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 14090, isZoneMission: true, ct);

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
            if (targetId == OriataPastNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return var == 0 && await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1)
                {
                    if (var != 0) return false;
                    SpawnQuestNpc(InstanceWorldId, player.Position.InstanceId, KaisinelNpc, 253f, 245f, 125f, 119);
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                }
                return false;
            }
            if (targetId == KaisinelNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return var == 1 && await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO2)
                {
                    if (var != 1) return false;
                    await GiveQuestItemAsync(player, conn, _itemDao, KaisinelItem, 1, ct);
                    // Skip: Java teleports the player to world 210050000 here via TeleportService2 (not ported).
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                }
                return false;
            }
            if (targetId == OutremusNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return var == 2 && await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SETPRO3)
                    return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
                return false;
            }
            if (targetId == EremitiaNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 3) return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                    if (var == 5) return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                    return false;
                }
                if (dialog == DialogAction.SETPRO4)
                    return await DefaultCloseDialogAsync(env, conn, 3, 4, ct);
                if (dialog == DialogAction.SETPRO6)
                    return await DefaultCloseDialogAsync(env, conn, 5, 6, ct);
                return false;
            }
            if (targetId == FasimedesNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return var == 4 && await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.SETPRO5)
                    return await DefaultCloseDialogAsync(env, conn, 4, 5, ct);
                return false;
            }
            if (targetId == GarnonNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return var == 6 && await SendQuestDialogAsync(conn, targetObjId, 3057, ct);
                if (dialog == DialogAction.SETPRO7)
                    return await DefaultCloseDialogAsync(env, conn, 6, 6, reward: true, sameNpc: false, ct);
                return false;
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == CrispinNpc) return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }

    public override ValueTask<bool> OnEnterWorldAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 0) return ValueTask.FromResult(false);
        if (player.Position.WorldId != InstanceWorldId) return ValueTask.FromResult(false);

        SpawnQuestNpc(InstanceWorldId, player.Position.InstanceId, OriataPastNpc, 255f, 245f, 125f, 55);
        return ValueTask.FromResult(false);
    }
}
