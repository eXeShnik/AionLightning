// Port of Java data/scripts/system/handlers/quest/oriel/_50009TisTheSeason.java (Bobobear).
// Talk to either 831032 or 831038 to start (repeatable — canRepeat() approximated as "no active
// entry", same simplification used across ~7 other zones); talking again while START unconditionally
// flips to REWARD (var 0->0, no dialog-action gate in Java) and shows dialog 2375; turn in at either
// npc.
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

namespace Quest.Oriel;

public sealed class _50009TisTheSeason : QuestHandlerBase
{
    private const int QuestIdConst = 50009;

    public _50009TisTheSeason(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(831032).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(831038).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(831032).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(831038).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (targetId is not (831032 or 831038)) return false;

        if (entry is null)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog is DialogAction.QUEST_ACCEPT_1 or DialogAction.QUEST_ACCEPT_SIMPLE)
                return await SendQuestStartDialogAsync(env, conn, ct);
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            await ChangeQuestStepAsync(conn, entry, 0, 0, toReward: true, ct);
            return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
        }

        if (entry.Status == QuestStatus.REWARD)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }
}
