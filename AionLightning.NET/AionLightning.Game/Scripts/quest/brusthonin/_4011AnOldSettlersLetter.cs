// Port of Java data/scripts/system/handlers/quest/brusthonin/_4011AnOldSettlersLetter.java.
// Use the Old Settler's Letter object (730139) to start; report to 205132 (var 0->1), then to
// 203522 (var 1 -> REWARD); turn in back at 205132.
// Java's switch(dialog) on 205132/203522 falls through from QUEST_SELECT into the next case
// without a break; both branches are guarded by the same var check so the fallthrough is a no-op —
// ported as plain if-checks with identical reachable behavior (see 4038 for the same pattern).
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

namespace Quest.Brusthonin;

public sealed class _4011AnOldSettlersLetter : QuestHandlerBase
{
    private const int QuestIdConst = 4011;
    private const int LetterObj    = 730139;
    private const int ReporterNpc  = 205132;
    private const int FinalNpc     = 203522;

    public _4011AnOldSettlersLetter(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(LetterObj).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(LetterObj).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ReporterNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(FinalNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (targetId == LetterObj)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog == DialogAction.SETPRO1)
            {
                await StartMissionAsync(conn, player, QuestStatus.START, ct);
                await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 0), ct);
                return true;
            }
        }

        var entry = player.Quests.Get(QuestId);
        if (entry is null) return false;
        int var = entry.GetVar(0);

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == ReporterNpc)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
                if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
            return false;
        }
        if (entry.Status != QuestStatus.START) return false;

        if (targetId == ReporterNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 0)
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            if (dialog == DialogAction.SETPRO2 && var == 0)
            {
                entry.SetVar(0, var + 1);
                await UpdateQuestStatusAsync(conn, entry, ct);
                await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                return true;
            }
            return false;
        }
        if (targetId == FinalNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 1)
                return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
            if (dialog == DialogAction.SET_SUCCEED && var == 1)
            {
                entry.Status = QuestStatus.REWARD;
                await UpdateQuestStatusAsync(conn, entry, ct);
                await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                return true;
            }
            return false;
        }
        return false;
    }
}
