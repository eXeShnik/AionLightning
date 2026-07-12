// Port of Java data/scripts/system/handlers/quest/sarpan/_20051EmpiricalTruth.java (vlog).
// Talk to Kutos (205581) to start; Aimah (205617) var 1->2; Philmoni (205765) var 2->3, then
// hands in the quest_data.xml collect-items at var 3->4; Ispharel (205988) flips to REWARD;
// turn in at Aimah.
// Fix vs Java: Java's register() never adds the "Mysterious Orb" (730465) to the OnTalk index, so
// its onDialogEvent case is dead code in the original (never dispatched) - registered here so the
// branch is reachable.
// Skip vs Java: that same 730465 branch calls TeleportService2.teleportTo(...) - no TeleportService
// exists in this port, so the interaction is a no-op past var 4 (the player must walk to Ispharel
// instead of using the shortcut). Not required for completability.
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

namespace Quest.Sarpan;

public sealed class _20051EmpiricalTruth : QuestHandlerBase
{
    private const int QuestIdConst   = 20051;
    private const int KutosNpc       = 205581;
    private const int AimahNpc       = 205617;
    private const int PhilmoniNpc    = 205765;
    private const int MysteriousOrbNpc = 730465;
    private const int IspharelNpc    = 205988;
    private const int SarpanWorldId  = 300390000;

    private readonly IItemDao _itemDao;

    public _20051EmpiricalTruth(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnEnterWorld(QuestId);
        engine.RegisterQuestNpc(KutosNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(AimahNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(PhilmoniNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(MysteriousOrbNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(IspharelNpc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 20050, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null) return false;

        var dialog      = DialogActionLookup.FromId(env.DialogId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);

            if (targetId == KutosNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1) return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }
            if (targetId == AimahNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO2) return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                return false;
            }
            if (targetId == PhilmoniNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 2) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                    if (var == 3) return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                    return false;
                }
                if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                    return await CheckQuestItemsAsync(env, conn, _itemDao, 3, 4, reward: false, checkOkId: 10000, checkFailId: 10001, ct);
                if (dialog == DialogAction.SETPRO3) return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
                if (dialog == DialogAction.FINISH_DIALOG) return await CloseDialogWindowAsync(conn, targetObjId, ct);
                return false;
            }
            if (targetId == MysteriousOrbNpc)
            {
                if (dialog == DialogAction.USE_OBJECT && var == 4)
                {
                    // Skip vs Java: TeleportService2.teleportTo(...) to Sarpan - no TeleportService in this port.
                    return true;
                }
                return false;
            }
            if (targetId == IspharelNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 4) return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.SET_SUCCEED) return await DefaultCloseDialogAsync(env, conn, 4, 4, reward: true, sameNpc: false, ct);
                return false;
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == AimahNpc)
        {
            if (dialog == DialogAction.USE_OBJECT) return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }

    public override ValueTask<bool> OnEnterWorldAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return ValueTask.FromResult(false);
        if (player.Position.WorldId != SarpanWorldId || entry.GetVar(0) != 4) return ValueTask.FromResult(false);

        return PlayMovieAsync(conn, player, ct);
    }

    private async ValueTask<bool> PlayMovieAsync(GsClientConnection conn, Player player, CancellationToken ct)
    {
        await PlayQuestMovieAsync(conn, player, 703, ct);
        return true;
    }
}
