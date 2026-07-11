// Port of Java data/scripts/system/handlers/quest/beluslan/_2641PowwowWiththeMau.java (Akiro).
// Talk to 204817 to start; relay chain 204795 (var 0->1) -> 204798 (var 1->2) -> 204796
// (var 2->3); turn in at 204700, which resets var 0 to 3 before flipping to REWARD.
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

namespace Quest.Beluslan;

public sealed class _2641PowwowWiththeMau : QuestHandlerBase
{
    private const int QuestIdConst = 2641;
    private const int StartNpc     = 204817;
    private const int Relay1Npc    = 204795;
    private const int Relay2Npc    = 204798;
    private const int Relay3Npc    = 204796;
    private const int FinalNpc     = 204700;

    public _2641PowwowWiththeMau(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(Relay1Npc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Relay2Npc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Relay3Npc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(FinalNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (targetId == StartNpc)
        {
            if (entry is null)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry is not null && entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);

            if (targetId == Relay1Npc && var == 0)
                return await RelayStepAsync(env, conn, entry, targetObjId, dialog, nextVar: 1, ct);
            if (targetId == Relay2Npc && var == 1)
                return await RelayStepAsync(env, conn, entry, targetObjId, dialog, nextVar: 2, ct);
            if (targetId == Relay3Npc && var == 2)
                return await RelayStepAsync(env, conn, entry, targetObjId, dialog, nextVar: 3, ct);
        }

        if (targetId == FinalNpc)
        {
            if (entry is null) return false;

            if (dialog == DialogAction.QUEST_SELECT && entry.Status == QuestStatus.START)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);

            if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD
                && entry.Status != QuestStatus.COMPLETE && entry.Status != QuestStatus.NONE)
            {
                entry.SetVar(0, 3);
                entry.Status = QuestStatus.REWARD;
                await UpdateQuestStatusAsync(conn, entry, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }

            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }

    // Java repeats this exact block (SELECT->1352, SETPRO1->bump+page10, else->sendQuestStartDialog)
    // once per relay npc; factored here since it is identical for all three relay steps.
    private async ValueTask<bool> RelayStepAsync(QuestEnv env, GsClientConnection conn, QuestEntry entry, int targetObjId,
        DialogAction dialog, int nextVar, CancellationToken ct)
    {
        if (dialog == DialogAction.QUEST_SELECT)
            return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
        if (dialog == DialogAction.SETPRO1)
        {
            entry.SetVar(0, nextVar);
            await UpdateQuestStatusAsync(conn, entry, ct);
            return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
        }
        return await SendQuestStartDialogAsync(env, conn, ct);
    }
}
