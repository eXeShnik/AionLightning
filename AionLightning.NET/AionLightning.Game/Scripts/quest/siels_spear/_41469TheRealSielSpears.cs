// Port of Java data/scripts/system/handlers/quest/siels_spear/_41469TheRealSielSpears.java (Cheatkiller).
// Single-npc relay quest: talk to 205579 to start (var 0), same npc flips straight to REWARD via
// SELECT_QUEST_REWARD (defaultCloseDialog with reward+sameNpc), then turns in.
// Java quirk preserved as a no-op: the Java switch's QUEST_SELECT case falls through into
// SELECT_QUEST_REWARD (no break) when var != 0 - unreachable in practice, see
// _11523TheShiningSpear's header for why.
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

namespace Quest.SielsSpear;

public sealed class _41469TheRealSielSpears : QuestHandlerBase
{
    private const int QuestIdConst = 41469;
    private const int Npc          = 205579;

    public _41469TheRealSielSpears(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
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
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId == Npc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
        }
        else if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);
            if (targetId == Npc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.SELECT_QUEST_REWARD)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, reward: true, sameNpc: true, ct);
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == Npc)
            {
                if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }
        return false;
    }
}
