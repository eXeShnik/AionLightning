// Port of Java data/scripts/system/handlers/quest/eltnen/_1470HannetsVengeance.java (Atomics).
// Talk to Hannet (790004) to start; killing Kromede (212846) flips it straight to REWARD.
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

namespace Quest.Eltnen;

public sealed class _1470HannetsVengeance : QuestHandlerBase
{
    private const int QuestIdConst = 1470;
    private const int HannetNpc    = 790004;
    private const int KromedeNpc   = 212846;

    public _1470HannetsVengeance(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(HannetNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(HannetNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(KromedeNpc).OnKill.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        if (env.TargetId != HannetNpc) return false;
        var entry = env.Player.Quests.Get(QuestId);
        int targetObjId = env.Target?.ObjectId ?? 0;

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (DialogActionLookup.FromId(env.DialogId) == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }
        if (entry.Status == QuestStatus.REWARD)
            return await SendQuestEndDialogAsync(env, conn, ct);
        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (env.TargetId != KromedeNpc) return false;

        await ChangeQuestStepAsync(conn, entry, 0, entry.GetVar(0) + 1, toReward: true, ct);
        return true;
    }
}
