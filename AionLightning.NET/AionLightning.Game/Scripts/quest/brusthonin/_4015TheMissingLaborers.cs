// Port of Java data/scripts/system/handlers/quest/brusthonin/_4015TheMissingLaborers.java.
// Talk to 205130 to start; use the Cart (730107, var 0->1); report back to 205130 to finish.
// Java bug fixed: the switch(targetId) block had no `break` between the 730107 and 205130
// cases, so an unmatched dialog action at 730107 fell through into 205130's completion logic
// (SELECT_QUEST_REWARD et al) using the wrong npc's object id — split into separate guarded
// per-npc branches instead (730107 only handles its own USE_OBJECT progress step).
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

namespace Quest.Brusthonin;

public sealed class _4015TheMissingLaborers : QuestHandlerBase
{
    private const int QuestIdConst = 4015;
    private const int StartNpc     = 205130;
    private const int CartObj      = 730107;

    public _4015TheMissingLaborers(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(CartObj).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null)
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
            if (targetId == CartObj)
            {
                if (entry.GetVar(0) == 0 && dialog == DialogAction.USE_OBJECT)
                    return await UseQuestObjectAsync(env, conn, 0, 1, false, false, ct);
                return false;
            }
            if (targetId == StartNpc && entry.GetVar(0) == 1)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
                {
                    entry.Status = QuestStatus.REWARD;
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestEndDialogAsync(env, conn, ct);
                }
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == StartNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }
}
