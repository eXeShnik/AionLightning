// Port of Java data/scripts/system/handlers/quest/fort_tiamat/_30765StopTheDarkPoetaCase.java (Cheatkiller).
// Mirror of _30715AnuhartLess: accept at 800071 (page 1011, var slot 5 = 1 on start — see
// _30702DredgionControlCenterAssault's header for the packed-int note). 205846 (sets var slot 5 to
// 2, an alternate-branch marker never read again in this file) and 205866 (resets all vars back to
// 0) can be visited in either order; kill 214904 once while var0==0 -> reward. Turn in at 800071.
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

public sealed class _30765StopTheDarkPoetaCase : QuestHandlerBase
{
    private const int QuestIdConst = 30765;
    private const int StartNpc     = 800071;
    private const int FirstNpc     = 205846;
    private const int SecondNpc    = 205866;
    private const int KillNpc      = 214904;

    public _30765StopTheDarkPoetaCase(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(FirstNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(SecondNpc).OnTalk.Add(QuestId);
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

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == FirstNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO1)
                {
                    entry.SetVar(5, 2);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await CloseDialogWindowAsync(conn, targetObjId, ct);
                }
                return false;
            }
            if (targetId == SecondNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SETPRO2)
                {
                    ResetVars(entry);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await CloseDialogWindowAsync(conn, targetObjId, ct);
                }
                return false;
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == StartNpc)
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
