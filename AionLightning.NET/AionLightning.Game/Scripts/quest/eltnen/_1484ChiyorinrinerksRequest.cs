// Port of Java data/scripts/system/handlers/quest/eltnen/_1484ChiyorinrinerksRequest.java (MrPoke, remod).
// Talk to 798127 to start; a 3-npc relay (204045 -> 204048 -> 204011) advances var 0 from 0 to 3;
// turn in at 798126.
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

namespace Quest.Eltnen;

public sealed class _1484ChiyorinrinerksRequest : QuestHandlerBase
{
    private const int QuestIdConst = 1484;
    private const int StartNpc     = 798127;
    private const int SecondNpc    = 204045;
    private const int ThirdNpc     = 204048;
    private const int FourthNpc    = 204011;
    private const int TurnInNpc    = 798126;

    public _1484ChiyorinrinerksRequest(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(SecondNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ThirdNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(FourthNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        int targetId = env.TargetId;
        var entry = env.Player.Quests.Get(QuestId);
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (targetId == StartNpc)
        {
            if (entry is null || entry.Status == QuestStatus.NONE)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (targetId == SecondNpc)
        {
            if (entry is not { Status: QuestStatus.START } || entry.GetVar(0) != 0) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            if (dialog == DialogAction.SETPRO1)
            {
                entry.SetVar(0, 1);
                await UpdateQuestStatusAsync(conn, entry, ct);
                await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                return true;
            }
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (targetId == ThirdNpc)
        {
            if (entry is not { Status: QuestStatus.START } || entry.GetVar(0) != 1) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            if (dialog == DialogAction.SETPRO1)
            {
                entry.SetVar(0, 2);
                await UpdateQuestStatusAsync(conn, entry, ct);
                await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 1693), ct);
                return true;
            }
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (targetId == FourthNpc)
        {
            if (entry is not { Status: QuestStatus.START } || entry.GetVar(0) != 2) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            if (dialog == DialogAction.SETPRO1)
            {
                entry.SetVar(0, 3);
                await UpdateQuestStatusAsync(conn, entry, ct);
                await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 1693), ct);
                return true;
            }
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (targetId == TurnInNpc)
        {
            if (entry is null) return false;
            if (dialog == DialogAction.QUEST_SELECT && entry.Status == QuestStatus.START)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD && entry.Status is not (QuestStatus.COMPLETE or QuestStatus.NONE))
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
