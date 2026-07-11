// Port of Java data/scripts/system/handlers/quest/beluslan/_4501DelayedOrder.java (Ritsu).
// Talk to 204728 to start and turn in; relay 1 at 204340 (var 0->1); relay 2 at 204348
// (var 1->2); back at 204728, reaching var 2 flips to REWARD on the next QUEST_SELECT click.
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

public sealed class _4501DelayedOrder : QuestHandlerBase
{
    private const int QuestIdConst = 4501;
    private const int StartFinalNpc = 204728;
    private const int Relay1Npc     = 204340;
    private const int Relay2Npc     = 204348;

    public _4501DelayedOrder(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartFinalNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartFinalNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Relay1Npc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Relay2Npc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (targetId == StartFinalNpc)
        {
            if (entry is null)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }

            if (entry.Status == QuestStatus.START)
            {
                int var = entry.GetVar(0);
                if (dialog == DialogAction.QUEST_SELECT && var == 2)
                {
                    entry.Status = QuestStatus.REWARD;
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                }
                if (dialog is DialogAction.QUEST_SELECT or DialogAction.SELECT_QUEST_REWARD)
                    return await SendQuestEndDialogAsync(env, conn, ct);
                return false;
            }

            if (entry.Status == QuestStatus.REWARD)
                return await SendQuestEndDialogAsync(env, conn, ct);

            return false;
        }

        if (entry is not null && entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);

            if (targetId == Relay1Npc && var == 0)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO1)
                {
                    entry.SetVar(0, 1);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                }
                return false;
            }

            if (targetId == Relay2Npc && var == 1)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SETPRO2)
                {
                    entry.SetVar(0, 2);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                }
                return false;
            }
        }

        return false;
    }
}
