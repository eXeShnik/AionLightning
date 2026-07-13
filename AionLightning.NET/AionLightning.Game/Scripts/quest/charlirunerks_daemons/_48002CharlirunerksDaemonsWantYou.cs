// Port of Java data/scripts/system/handlers/quest/charlirunerks_daemons/_48002CharlirunerksDaemonsWantYou.java (vlog).
// ASMODIAN "Charlirunerk's Daemons" faction quest: auto-starts on level-up at minlevel 50
// (registerOnLevelUp -> QuestService.startQuest, ported through the shared DefaultOnLvlUpEventAsync
// which applies the template's min-level + race gate). Talk to Tikalanerk (799886): QUEST_SELECT
// shows dialog 10002; SELECT_QUEST_REWARD flips straight to REWARD (changeQuestStep 0,0,true) then
// shows dialog 5; turn in back at Tikalanerk.
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

namespace Quest.CharlirunerksDaemons;

public sealed class _48002CharlirunerksDaemonsWantYou : QuestHandlerBase
{
    private const int QuestIdConst   = 48002;
    private const int TikalanerkNpc  = 799886;

    public _48002CharlirunerksDaemonsWantYou(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(TikalanerkNpc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry       = env.Player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (targetId != TikalanerkNpc) return false;

        if (entry.Status == QuestStatus.START)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
            {
                await ChangeQuestStepAsync(conn, entry, 0, 0, toReward: true, ct);
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }
}
