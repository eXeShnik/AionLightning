// Port of Java data/scripts/system/handlers/quest/pandaemonium/_2937UnexpectedReward.java.
// Accept default at Talon (204092); at Nekorunuerk (798059) the Java switch falls through from
// QUEST_SELECT (when var != 0) into SELECT_QUEST_REWARD — dead in practice since var only ever
// becomes non-zero via that same reward branch, but ported 1:1 via the explicit OR below.
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

namespace Quest.Pandaemonium;

public sealed class _2937UnexpectedReward : QuestHandlerBase
{
    private const int QuestIdConst = 2937;
    private const int TalonNpc = 204092;
    private const int NekorunuerkNpc = 798059;

    public _2937UnexpectedReward(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(TalonNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(TalonNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(NekorunuerkNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null)
        {
            if (targetId != TalonNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId != NekorunuerkNpc) return false;
            int var = entry.GetVar(0);

            if (dialog == DialogAction.SELECT_QUEST_REWARD || (dialog == DialogAction.QUEST_SELECT && var != 0))
            {
                await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: true, ct);
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            }
            if (dialog == DialogAction.QUEST_SELECT && var == 0)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == NekorunuerkNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }
}
