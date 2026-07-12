// Port of Java data/scripts/system/handlers/quest/rider_quests/_14024AKrallingSuspicion.java (pralinka).
// Zone-mission sub-quest of 14020: talk to 203904 (var0 0->1), 204045 (var0 1->2), collect-check
// at 204004 flips straight to REWARD, turn in at 204020.
// Skip vs Java: a TeleportService2.teleportTo call at 204045's SETPRO2 relocates the player
// mid-chain — no TeleportService2 exists in this port (same precedent as
// quest/eltnen/_1036KaidanPrisoner.cs). The var/status transitions are kept.
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

public sealed class _14024AKrallingSuspicion : QuestHandlerBase
{
    private const int QuestIdConst = 14024;
    private const int Npc203904 = 203904;
    private const int Npc204045 = 204045;
    private const int Npc204004 = 204004;
    private const int Npc204020 = 204020;

    private readonly IItemDao _itemDao;

    public _14024AKrallingSuspicion(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        foreach (int npc in new[] { Npc203904, Npc204045, Npc204004, Npc204020 })
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, 14020, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null) return false;
        if (entry.Status != QuestStatus.START)
            return entry.Status == QuestStatus.REWARD && env.TargetId == Npc204020 && await SendQuestEndDialogAsync(env, conn, ct);

        var dialog = DialogActionLookup.FromId(env.DialogId);
        int targetObjId = env.Target?.ObjectId ?? 0;
        int var = entry.GetVar(0);

        if (env.TargetId == Npc203904)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return var == 0 && await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog == DialogAction.SETPRO1)
                return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
            return false;
        }

        if (env.TargetId == Npc204045)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return var == 1 && await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            if (dialog == DialogAction.SETPRO2)
                return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
            return false;
        }

        if (env.TargetId == Npc204004)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return var == 2 && await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
            if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                return var == 2 && await CheckQuestItemsAsync(env, conn, _itemDao, 2, 2, reward: true, checkOkId: 10, checkFailId: 0, ct);
            if (dialog == DialogAction.FINISH_DIALOG)
                return await CloseDialogWindowAsync(conn, targetObjId, ct);
            return false;
        }

        return false;
    }
}
