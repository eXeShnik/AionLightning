// Port of Java data/scripts/system/handlers/quest/brusthonin/_4036HeirnirWantstoRest.java.
// Talk to 205187 to start; report to 205144 (var 0->1); final check-in at 205190 flags var 3 and
// flips to REWARD. 205187 also accepts an immediate SELECT_QUEST_REWARD while START (ported
// faithfully as written, same shape as 4001).
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

public sealed class _4036HeirnirWantstoRest : QuestHandlerBase
{
    private const int QuestIdConst = 4036;
    private const int HeirnirNpc   = 205187;
    private const int ReporterNpc  = 205144;
    private const int FinalNpc     = 205190;

    public _4036HeirnirWantstoRest(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(HeirnirNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(HeirnirNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ReporterNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(FinalNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (targetId == HeirnirNpc)
        {
            if (entry is null)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            if (entry.Status == QuestStatus.START)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD && entry.Status != QuestStatus.COMPLETE)
                {
                    entry.Status = QuestStatus.REWARD;
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                    return true;
                }
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
            if (entry.Status == QuestStatus.REWARD)
                return await SendQuestEndDialogAsync(env, conn, ct);
            return false;
        }

        if (targetId == ReporterNpc)
        {
            if (entry is not null && entry.Status == QuestStatus.START && entry.GetVar(0) == 0)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO1)
                {
                    entry.SetVar(0, entry.GetVar(0) + 1);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                    return true;
                }
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (targetId == FinalNpc)
        {
            if (entry is null) return false;
            if (dialog == DialogAction.QUEST_SELECT && entry.Status == QuestStatus.START)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD && entry.Status != QuestStatus.COMPLETE)
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
}
