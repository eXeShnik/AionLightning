// Port of Java data/scripts/system/handlers/quest/reshanta/_1761SohonerkWish.java (Cheatkiller).
// Two-outcome quest resolved entirely at the start npc 279014: SETPRO10 -> reward tier 0,
// SETPRO20 -> reward tier 1 (Java tracked the chosen tier in a mutable instance field `rewardIndex`
// shared across every player using this singleton handler — a latent concurrency/correctness bug,
// fixed here by deriving the tier from the persisted quest var (var0 1 -> tier 0, var0 2 -> tier 1)
// instead). 279017/279018 are flavor npcs shown via USE_OBJECT while REWARD; the actual turn-in
// (any other interaction while REWARD, including at 279014 itself) finishes via the reward-index
// path, matching the established Insomnia Medicine convention (SELECTED_QUEST_NOREWARD -> finish).
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.Reshanta;

public sealed class _1761SohonerkWish : QuestHandlerBase
{
    private const int QuestIdConst = 1761;
    private const int StartNpc     = 279014;
    private const int FlavorNpc1   = 279017;
    private const int FlavorNpc2   = 279018;

    public _1761SohonerkWish(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(FlavorNpc1).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(FlavorNpc2).OnTalk.Add(QuestId);
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
            if (targetId != StartNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START && targetId == StartNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1003, ct);
            if (dialog == DialogAction.SELECT_ACTION_1012)
                return await SendQuestDialogAsync(conn, targetObjId, 1012, ct);
            if (dialog == DialogAction.SELECT_ACTION_1097)
                return await SendQuestDialogAsync(conn, targetObjId, 1097, ct);
            if (dialog == DialogAction.SETPRO10)
            {
                await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: true, ct);
                return await CloseDialogWindowAsync(conn, targetObjId, ct);
            }
            if (dialog == DialogAction.SETPRO20)
            {
                await ChangeQuestStepAsync(conn, entry, 0, 2, toReward: true, ct);
                return await CloseDialogWindowAsync(conn, targetObjId, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == FlavorNpc1 && dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            if (targetId == FlavorNpc2 && dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);

            int rewardIndex = entry.GetVar(0) - 1;
            if (env.DialogId == (int)DialogAction.SELECTED_QUEST_NOREWARD)
            {
                await FinishQuestAsync(conn, player, rewardIndex, ct);
                await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                return true;
            }
            if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
                return await SendQuestDialogAsync(conn, targetObjId, 5 + rewardIndex, ct);
        }

        return false;
    }
}
