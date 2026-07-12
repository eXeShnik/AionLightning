// Port of Java data/scripts/system/handlers/quest/tiamaranta/_41546JumpingToConclusions.java (Cheatkiller).
// Talk to 205969 to start; talk to 205915 to receive an item (var 0->1); return the item to
// 205969 to turn in.
// Java bug fixed: onDialogEvent's switch on 205969 has "case QUEST_SELECT: {...} case
// SELECT_QUEST_REWARD: {removeQuestItem(...); ...}" with no break, so a QUEST_SELECT click at any
// var other than 1 falls through and removes the quest item / attempts the reward transition
// unconditionally (DefaultCloseDialogAsync's own step guard would then silently swallow the
// no-op, but the item removal already happened) — ported as an explicit reward-status check
// instead of relying on switch fallthrough.
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

namespace Quest.Tiamaranta;

public sealed class _41546JumpingToConclusions : QuestHandlerBase
{
    private const int QuestIdConst = 41546;
    private const int StartNpc     = 205969;
    private const int MidNpc       = 205915;
    private const int TokenItemId  = 182212544;

    private readonly IItemDao _itemDao;

    public _41546JumpingToConclusions(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(MidNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId != StartNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == MidNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO1)
                {
                    await GiveQuestItemAsync(player, conn, _itemDao, TokenItemId, 1, ct);
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                }
                return false;
            }
            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && entry.GetVar(0) == 1)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.SELECT_QUEST_REWARD)
                {
                    await RemoveQuestItemAsync(player, conn, _itemDao, TokenItemId, 1, ct);
                    return await DefaultCloseDialogAsync(env, conn, 1, 1, reward: true, sameNpc: true, ct);
                }
                return false;
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == StartNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }
}
