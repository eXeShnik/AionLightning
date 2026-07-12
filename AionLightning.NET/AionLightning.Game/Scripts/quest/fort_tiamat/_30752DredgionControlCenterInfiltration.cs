// Port of Java data/scripts/system/handlers/quest/fort_tiamat/_30752DredgionControlCenterInfiltration.java (Cheatkiller).
// Asmodian mirror of _30702DredgionControlCenterAssault, offered from the same 800424 hub npc:
// accept (page 1011, var slot 5 = 1 on start — see _30702's header for the packed-int note); use
// 730702 (USE_OBJECT, resets all vars back to 0); kill 219354 once (var0==0 -> reward). Turn in at
// 800072 (vs. 800067 for the Elyos-side quest).
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.FortTiamat;

public sealed class _30752DredgionControlCenterInfiltration : QuestHandlerBase
{
    private const int QuestIdConst = 30752;
    private const int StartNpc     = 800424;
    private const int UseNpc       = 730702;
    private const int TurnInNpc    = 800072;
    private const int KillNpc      = 219354;

    public _30752DredgionControlCenterInfiltration(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(UseNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(KillNpc).OnKill.Add(QuestId);
    }

    public override ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnKillEventAsync(env, conn, KillNpc, 0, reward: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId != StartNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog == DialogAction.QUEST_ACCEPT_SIMPLE)
                return await StartWithFlagAsync(conn, player, targetObjId, ct);
            return false;
        }

        if (entry.Status == QuestStatus.START && targetId == UseNpc)
        {
            switch (dialog)
            {
                case DialogAction.USE_OBJECT:
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                case DialogAction.SETPRO1:
                    ResetVars(entry);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await CloseDialogWindowAsync(conn, targetObjId, ct);
                default:
                    return false;
            }
        }

        if (entry.Status == QuestStatus.REWARD && targetId == TurnInNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }

    /// <summary>Java's <c>sendQuestStartDialog(env, 1073741824)</c>: creates the entry directly at
    /// START with quest var slot 5 set to 1 (the packed-int equivalent of 64^5).</summary>
    private async ValueTask<bool> StartWithFlagAsync(GsClientConnection conn, Player player, int targetObjId, CancellationToken ct)
    {
        if (player.Quests.Get(QuestId) is not null) return false;

        var entry = new QuestEntry { QuestId = QuestId, Status = QuestStatus.START };
        entry.SetVar(5, 1);
        player.Quests.Add(entry);
        await QuestDao.UpsertAsync(player.ObjectId, entry, ct);
        await conn.SendAsync(new SM_QUEST_ACTION(entry.QuestId,
            SM_QUEST_ACTION.ActionType.Accept, (byte)entry.Status, entry.Step), ct);
        return await CloseDialogWindowAsync(conn, targetObjId, ct);
    }

    /// <summary>Java's <c>qs.setQuestVar(0)</c>: decodes the packed value 0 into every var slot.</summary>
    private static void ResetVars(QuestEntry entry)
    {
        for (int i = 0; i < 6; i++) entry.SetVar(i, 0);
    }
}
