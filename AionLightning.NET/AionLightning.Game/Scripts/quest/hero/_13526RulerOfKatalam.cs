// Port of Java data/scripts/system/handlers/quest/hero/_13526RulerOfKatalam.java.
// Auto-starts on level-up; accept at Tirins (800527), turn in at Pennan (800845) —
// SELECT_QUEST_REWARD flips straight to REWARD (var 0 unchanged) and shows the completion dialog.
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

namespace Quest.Hero;

public sealed class _13526RulerOfKatalam : QuestHandlerBase
{
    private const int QuestIdConst = 13526;
    private const int TirinsNpc    = 800527;
    private const int PennanNpc    = 800845;

    public _13526RulerOfKatalam(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(TirinsNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(TirinsNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(PennanNpc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId != TirinsNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog is DialogAction.QUEST_ACCEPT_1 or DialogAction.QUEST_ACCEPT_SIMPLE)
                return await SendQuestStartDialogAsync(env, conn, ct);
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId != PennanNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            if (dialog == DialogAction.SELECT_QUEST_REWARD)
            {
                await ChangeQuestStepAsync(conn, entry, 0, 0, toReward: true, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == PennanNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }
}
