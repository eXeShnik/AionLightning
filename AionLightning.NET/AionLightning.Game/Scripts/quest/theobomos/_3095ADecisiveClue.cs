// Port of Java data/scripts/system/handlers/quest/theobomos/_3095ADecisiveClue.java.
// Accepted from the nearby-quests panel (targetId 0, QUEST_ACCEPT_1); talking to the Red Journal
// object (730148) grants the clue item (182208053); Ariel (798225, var 0->1) then a hand-off
// (203898, var 1->2, consumes the clue) advance the chain; return to Ariel to flip to REWARD and
// turn in.
// Skip vs Java: onItemUseEvent is dead code in the Java source (no registerQuestItem call is ever
// made for this quest, so the engine never dispatches an item-use event to it) - omitted here.
// Skip vs Java: case 798225 falls through (no break) into case 203898 when none of its own
// var==0/2/REWARD branches match - omitted since it would let a stray dialog id on Ariel trigger
// the 203898 hand-off logic; written here as two independent target blocks instead.
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

namespace Quest.Theobomos;

public sealed class _3095ADecisiveClue : QuestHandlerBase
{
    private const int QuestIdConst  = 3095;
    private const int RedJournalObj = 730148;
    private const int ArielNpc      = 798225;
    private const int HandoffNpc    = 203898;
    private const int ClueItemId    = 182208053;

    private readonly IItemDao _itemDao;

    public _3095ADecisiveClue(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(RedJournalObj).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(RedJournalObj).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ArielNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(HandoffNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;

        if ((entry is null || entry.Status == QuestStatus.NONE) && targetId == 0)
        {
            if (env.DialogId == (int)DialogAction.QUEST_ACCEPT_1)
            {
                await StartMissionAsync(conn, player, QuestStatus.START, ct);
                return await CloseDialogWindowAsync(conn, targetObjId, ct);
            }
            return false;
        }

        if ((entry is null || entry.Status == QuestStatus.NONE) && targetId == RedJournalObj)
            return await GiveQuestItemAsync(player, conn, _itemDao, ClueItemId, 1, ct);

        if (entry is null) return false;

        var dialog = DialogActionLookup.FromId(env.DialogId);
        int var = entry.GetVar(0);

        if (targetId == ArielNpc)
        {
            if (entry.Status == QuestStatus.START && var == 0)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO1)
                {
                    entry.SetVar(0, var + 1);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                    return true;
                }
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            if (entry.Status == QuestStatus.START && var == 2)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
                {
                    entry.Status = QuestStatus.REWARD;
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                }
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            if (entry.Status == QuestStatus.REWARD)
                return await SendQuestEndDialogAsync(env, conn, ct);
        }
        else if (targetId == HandoffNpc && entry.Status == QuestStatus.START && var == 1)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
            if (dialog == DialogAction.SETPRO2)
            {
                await RemoveQuestItemAsync(player, conn, _itemDao, ClueItemId, 1, ct);
                entry.SetVar(0, var + 1);
                await UpdateQuestStatusAsync(conn, entry, ct);
                await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                return true;
            }
            return await SendQuestStartDialogAsync(env, conn, ct);
        }
        return false;
    }
}
