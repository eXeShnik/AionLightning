// Port of Java data/scripts/system/handlers/quest/siels_spear/_11523TheShiningSpear.java (Cheatkiller).
// Talk to the Shining Spear (205579) to start; relay through 205581 (var 0->1, reward) and turn in
// back at 205579.
// Java quirk preserved as a no-op: the Java switch's QUEST_SELECT case falls through into SETPRO1
// (no break) when var != 0 - unreachable in practice since var can only be 0 while status is START
// (the SETPRO1 branch itself is what advances it and flips to REWARD), so this port just handles
// QUEST_SELECT and SETPRO1 as separate conditions with identical observable behavior.
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

public sealed class _11523TheShiningSpear : QuestHandlerBase
{
    private const int QuestIdConst = 11523;
    private const int StartNpc     = 205579;
    private const int RelayNpc     = 205581;

    public _11523TheShiningSpear(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(RelayNpc).OnTalk.Add(QuestId);
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
            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
        }
        else if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);
            if (targetId == RelayNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO1)
                {
                    await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: true, ct);
                    return await CloseDialogWindowAsync(conn, targetObjId, ct);
                }
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == StartNpc)
            {
                if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }
        return false;
    }
}
