// Port of Java data/scripts/system/handlers/quest/verteron/_1156StolenVillageSeal.java
// (Rhys2002, reworked vlog). Talk to Santenius (203128) to start; use the Item Stack (700003,
// var 0 -> 1); turn in at Gaphyrk (798003).
using System.Linq;
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

namespace Quest.Verteron;

public sealed class _1156StolenVillageSeal : QuestHandlerBase
{
    private const int QuestIdConst  = 1156;
    private const int SanteniusNpc  = 203128;
    private const int ItemStackObj  = 700003;
    private const int GaphyrkNpc    = 798003;

    public _1156StolenVillageSeal(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(SanteniusNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(SanteniusNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ItemStackObj).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(GaphyrkNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        int targetId = env.TargetId;
        var entry = player.Quests.Get(QuestId);
        var dialog = DialogActionLookup.FromId(env.DialogId);
        int targetObjId = env.Target?.ObjectId ?? 0;

        if (entry is null)
        {
            if (targetId == SanteniusNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);
            if (targetId == ItemStackObj)
            {
                if (dialog == DialogAction.USE_OBJECT && var == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO1)
                {
                    await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: false, ct);
                    return await CloseDialogWindowAsync(conn, targetObjId, ct);
                }
                return false;
            }
            if (targetId == GaphyrkNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 1)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
                {
                    await ChangeQuestStepAsync(conn, entry, 1, 1, toReward: true, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                }
                return false;
            }
        }
        else if (entry.Status == QuestStatus.REWARD && targetId == GaphyrkNpc)
        {
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }
}
