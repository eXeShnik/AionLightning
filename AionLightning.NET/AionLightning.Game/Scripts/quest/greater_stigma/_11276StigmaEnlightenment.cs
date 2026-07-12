// Port of Java data/scripts/system/handlers/quest/greater_stigma/_11276StigmaEnlightenment.java (vlog).
// Talk to Reemul (798909) to start; a simple "select or reward-select while var 0" flips straight
// to REWARD (no collect-item check, unlike _11049 at the same npc); turn in there too.
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

namespace Quest.GreaterStigma;

public sealed class _11276StigmaEnlightenment : QuestHandlerBase
{
    private const int QuestIdConst = 11276;
    private const int ReemulNpc    = 798909;

    public _11276StigmaEnlightenment(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(ReemulNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(ReemulNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry  = env.Player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null)
        {
            if (targetId != ReemulNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START && targetId == ReemulNpc)
        {
            if ((dialog == DialogAction.QUEST_SELECT || dialog == DialogAction.SELECT_QUEST_REWARD) && entry.GetVar(0) == 0)
            {
                await ChangeQuestStepAsync(conn, entry, -1, 0, toReward: true, ct);
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == ReemulNpc)
        {
            if (dialog == DialogAction.SELECT_QUEST_REWARD)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
