// Port of Java data/scripts/system/handlers/quest/oriel/_18806HeartofRock.java (zhkchi).
// Talk to any of the 5 butler-template npcs (810017-810021, registered as plain start npcs — the
// script never checks House/ActiveHouse) to start; talk to 830528 (var 0->1 via SETPRO1, shown at
// dialog 1352/1353), then 830194 (var 1->1, reward, dialog 5) to flip to REWARD; turn in at 830194.
using System.Collections.Generic;
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

public sealed class _18806HeartofRock : QuestHandlerBase
{
    private const int QuestIdConst = 18806;
    private const int GinsengNpc   = 830528;
    private const int TurnInNpc    = 830194;

    private static readonly HashSet<int> Butlers = [810017, 810018, 810019, 810020, 810021];

    public _18806HeartofRock(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        foreach (int butlerId in Butlers)
        {
            engine.RegisterQuestNpc(butlerId).OnQuestStart.Add(QuestId);
            engine.RegisterQuestNpc(butlerId).OnTalk.Add(QuestId);
        }
        engine.RegisterQuestNpc(GinsengNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
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
            if (Butlers.Contains(targetId))
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog is DialogAction.QUEST_ACCEPT_1 or DialogAction.QUEST_ACCEPT_SIMPLE)
                    return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == GinsengNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SELECT_ACTION_1353)
                    return await SendQuestDialogAsync(conn, targetObjId, 1353, ct);
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }

            if (targetId == TurnInNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.SELECT_QUEST_REWARD)
                {
                    await ChangeQuestStepAsync(conn, entry, 1, 1, toReward: true, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                }
                return false;
            }

            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == TurnInNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }
}
