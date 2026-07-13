// Port of Java data/scripts/system/handlers/quest/gelkmaros_armor/_21054MissionofDestiny.java (keqi).
// Armor quest at 799318: multi-page accept (1011 -> 1012 -> page 4 -> QUEST_ACCEPT_1 starts /
// QUEST_REFUSE_1 shows 1004). In START, QUEST_SELECT shows 2375 and any other action falls through
// to sendQuestStartDialog (faithful to Java's `default: return sendQuestStartDialog(env)`). Turn in
// at 799318.
// note: Java's qs.canRepeat() daily-repeat re-entry is approximated as "no active entry".
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

namespace Quest.GelkmarosArmor;

public sealed class _21054MissionofDestiny : QuestHandlerBase
{
    private const int QuestIdConst = 21054;
    private const int Npc          = 799318;

    public _21054MissionofDestiny(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(Npc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(Npc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId == Npc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SELECT_ACTION_1012)
                    return await SendQuestDialogAsync(conn, targetObjId, 1012, ct);
                if (dialog == DialogAction.ASK_QUEST_ACCEPT)
                    return await SendQuestDialogAsync(conn, targetObjId, 4, ct);
                if (dialog == DialogAction.QUEST_ACCEPT_1)
                    return await SendQuestStartDialogAsync(env, conn, ct);
                if (dialog == DialogAction.QUEST_REFUSE_1)
                    return await SendQuestDialogAsync(conn, targetObjId, 1004, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == Npc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == Npc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }
}
