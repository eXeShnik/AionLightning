// Port of Java data/scripts/system/handlers/quest/event_quests/_80330TransformWithTheMagicCane.java.
// Single-npc accept/turn-in quest at 831527, repeatable (Java qs.canRepeat() gate approximated
// as "no active entry", matching the rest of this port — see QuestEngine.ComputeNearbyQuests).
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

namespace Quest.EventQuests;

public sealed class _80330TransformWithTheMagicCane : QuestHandlerBase
{
    private const int QuestIdConst = 80330;
    private const int CaneNpc      = 831527;

    public _80330TransformWithTheMagicCane(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(CaneNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(CaneNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        if (env.TargetId != CaneNpc) return false;

        var entry = env.Player.Quests.Get(QuestId);
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            return dialog switch
            {
                DialogAction.QUEST_SELECT => await SendQuestDialogAsync(conn, targetObjId, 1011, ct),
                DialogAction.QUEST_ACCEPT_1 or DialogAction.QUEST_ACCEPT_SIMPLE => await SendQuestStartDialogAsync(env, conn, ct),
                _ => false,
            };
        }

        if (entry.Status == QuestStatus.START)
        {
            if (dialog is DialogAction.QUEST_SELECT or DialogAction.SELECT_QUEST_REWARD)
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
