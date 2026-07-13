// Port of Java data/scripts/system/handlers/quest/radiant_ops/_38001RadiantOpsRecruitment.java (vlog).
// Auto-starts on a level-up tick once the player reaches level 40 (QuestService.startQuest ->
// StartMissionAsync, matching the katalam onEnterZone startQuest precedent). In START at Pompo
// (799828), QUEST_SELECT shows page 10002 and SELECT_QUEST_REWARD flips to REWARD (page 5); turn in
// at Pompo.
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

namespace Quest.RadiantOps;

public sealed class _38001RadiantOpsRecruitment : QuestHandlerBase
{
    private const int QuestIdConst = 38001;
    private const int PompoNpc     = 799828;

    public _38001RadiantOpsRecruitment(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(PompoNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (player.Level >= 40 && (entry is null || entry.Status == QuestStatus.NONE))
            return await StartMissionAsync(conn, player, QuestStatus.START, ct);
        return false;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.START && targetId == PompoNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            if (dialog == DialogAction.SELECT_QUEST_REWARD)
            {
                await ChangeQuestStepAsync(conn, entry, 0, 0, toReward: true, ct);
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == PompoNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }
}
