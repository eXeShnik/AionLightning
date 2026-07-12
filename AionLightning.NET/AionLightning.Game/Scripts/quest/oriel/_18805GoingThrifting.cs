// Port of Java data/scripts/system/handlers/quest/oriel/_18805GoingThrifting.java (zhkchi).
// Accept at 830070; talk to any of 830660/830661/830520 (var 0->1352 dialog, var 2->2375 dialog;
// SETPRO1 advances 0->1), then use-object at 730525/730522 (SETPRO2 advances 1->2), back to the
// first trio to turn in (SELECT_QUEST_REWARD: var 2->2, reward). Java's per-case switch blocks fall
// through into the next case with no `break` (e.g. QUEST_SELECT falling into SETPRO1), but every
// fallthrough target re-validates its own var precondition and is unreachable in practice once the
// preceding if/else already handled the live var value — ported as independent conditions with
// identical observable behavior (no behavior change, just no dead fallthrough).
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

public sealed class _18805GoingThrifting : QuestHandlerBase
{
    private const int QuestIdConst = 18805;
    private const int StartNpc     = 830070;

    public _18805GoingThrifting(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(830660).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(830661).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(830520).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(730525).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(730522).OnTalk.Add(QuestId);
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
            if (targetId == StartNpc)
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

            if (targetId is 830660 or 830661 or 830520)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    if (var == 2) return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                    return false;
                }
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                if (dialog == DialogAction.SELECT_QUEST_REWARD)
                {
                    await ChangeQuestStepAsync(conn, entry, 2, 2, toReward: true, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                }
                return false;
            }

            if (targetId is 730525 or 730522)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return var == 1 && await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SETPRO2)
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                return false;
            }

            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId is 830660 or 830661 or 830520)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }
}
