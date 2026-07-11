// Port of Java data/scripts/system/handlers/quest/verteron/_1192VerteronReinforcements.java
// (Balthazar). Talk to Santenius (203098) to start; advance at 203701 (var 0->1) then 203833
// (var 1->2); return to Santenius to finish (var ->3, REWARD).
using System.Linq;
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

namespace Quest.Verteron;

public sealed class _1192VerteronReinforcements : QuestHandlerBase
{
    private const int QuestIdConst  = 1192;
    private const int SanteniusNpc  = 203098;
    private const int FirstNpc      = 203701;
    private const int SecondNpc     = 203833;

    public _1192VerteronReinforcements(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(SanteniusNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(SanteniusNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(FirstNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(SecondNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        int targetId = env.TargetId;
        var entry = player.Quests.Get(QuestId);
        var dialog = DialogActionLookup.FromId(env.DialogId);
        int targetObjId = env.Target?.ObjectId ?? 0;

        if (entry is null)
        {
            if (targetId == SanteniusNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == FirstNpc)
            {
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT:
                        return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    case DialogAction.SETPRO1:
                        entry.SetVar(0, entry.GetVar(0) + 1);
                        await UpdateQuestStatusAsync(conn, entry, ct);
                        await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                        return true;
                    default:
                        return false;
                }
            }
            if (targetId == SecondNpc)
            {
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT:
                        return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                    case DialogAction.SETPRO2:
                        entry.SetVar(0, entry.GetVar(0) + 1);
                        await UpdateQuestStatusAsync(conn, entry, ct);
                        await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                        return true;
                    default:
                        return await SendQuestEndDialogAsync(env, conn, ct);
                }
            }
            if (targetId == SanteniusNpc)
            {
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT:
                        return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                    case DialogAction.SELECT_QUEST_REWARD:
                        entry.SetVar(0, 3);
                        entry.Status = QuestStatus.REWARD;
                        await UpdateQuestStatusAsync(conn, entry, ct);
                        await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                        return true;
                    default:
                        return await SendQuestEndDialogAsync(env, conn, ct);
                }
            }
        }
        else if (entry.Status == QuestStatus.REWARD && targetId == SanteniusNpc)
        {
            if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }
}
