// Port of Java data/scripts/system/handlers/quest/sanctum/_1908UlaguruSpeaks.java (Cheatkiller).
// Talk to Ulaguru (203864) to start. Java bug fix: the intermediate step checked
// `targetId == 203890`, an npc id that was never registered for OnTalk (only 204120 was
// registered), so the var 0->1 advance was unreachable and the quest could never progress in the
// original source; this port checks the actually-registered npc (204120) instead. Ulaguru then
// offers a 3-way branch (var 21/22/23) with distinct reward tiers via the reward-index finish.
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.Sanctum;

public sealed class _1908UlaguruSpeaks : QuestHandlerBase
{
    private const int QuestIdConst = 1908;
    private const int StartNpc     = 203864;
    private const int ReportNpc    = 204120;

    public _1908UlaguruSpeaks(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ReportNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry  = env.Player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == ReportNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && entry.GetVar(0) == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
            }
            else if (targetId == StartNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && entry.GetVar(0) == 1)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.SETPRO21)
                {
                    entry.SetVar(0, 21);
                    return await SendQuestDialogAsync(conn, targetObjId, 2376, ct);
                }
                if (dialog == DialogAction.SETPRO22)
                {
                    entry.SetVar(0, 22);
                    return await SendQuestDialogAsync(conn, targetObjId, 2461, ct);
                }
                if (dialog == DialogAction.SETPRO23)
                {
                    entry.SetVar(0, 23);
                    return await SendQuestDialogAsync(conn, targetObjId, 2546, ct);
                }
                if (dialog == DialogAction.SELECT_QUEST_REWARD)
                    return await DefaultCloseDialogAsync(env, conn, entry.GetVar(0), entry.GetVar(0), reward: true, sameNpc: true, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == StartNpc)
        {
            int var = entry.GetVar(0);
            if (var == 21) return await SendQuestEndDialogWithRewardAsync(env, conn, 0, ct);
            if (var == 22) return await SendQuestEndDialogWithRewardAsync(env, conn, 1, ct);
            if (var == 23) return await SendQuestEndDialogWithRewardAsync(env, conn, 2, ct);
        }

        return false;
    }

    /// <summary>Java QuestHandler.sendQuestEndDialog(env, reward): SELECT_QUEST_REWARD/USE_OBJECT
    /// shows the tier-confirm page 5+reward; SELECTED_QUEST_REWARDn/NOREWARD actually completes
    /// with that reward tier.</summary>
    private async ValueTask<bool> SendQuestEndDialogWithRewardAsync(QuestEnv env, GsClientConnection conn, int rewardIndex, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.REWARD) return false;
        int targetObjId = env.Target?.ObjectId ?? 0;
        int dialogId = env.DialogId;

        if (dialogId >= (int)DialogAction.SELECTED_QUEST_REWARD1 && dialogId <= (int)DialogAction.SELECTED_QUEST_NOREWARD)
        {
            if (!await FinishQuestAsync(conn, env.Player, rewardIndex, ct)) return false;
            await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
            return true;
        }
        if (dialogId == (int)DialogAction.SELECT_QUEST_REWARD || dialogId == (int)DialogAction.USE_OBJECT)
            return await SendQuestDialogAsync(conn, targetObjId, 5 + rewardIndex, ct);
        return false;
    }
}
