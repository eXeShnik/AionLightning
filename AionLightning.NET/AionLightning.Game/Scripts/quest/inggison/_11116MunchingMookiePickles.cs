// Port of Java data/scripts/system/handlers/quest/inggison/_11116MunchingMookiePickles.java.
// Talk to 798986 to start (no item); relay at 798964 (var 0->1); collect-check for 10x item
// 182206790 at 203784 (var 1) gives 182206791; turn-in trigger at 203785 (var 2) gives 182206792
// and flips to reward; turn in back at 798986.
// Java bug fixed: both the collect-check and the turn-in trigger advanced the var only when
// giveQuestItem FAILED (`if (!giveQuestItem(...)) var++`), stranding the quest forever on the
// common success path; ported as advance-on-success like every sibling give/remove step here.
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.Inggison;

public sealed class _11116MunchingMookiePickles : QuestHandlerBase
{
    private const int QuestIdConst = 11116;
    private const int StartNpc     = 798986;
    private const int Relay1Npc    = 798964;
    private const int Relay2Npc    = 203784;
    private const int TurnInNpc    = 203785;
    private const int CollectItem  = 182206790;
    private const int TokenItem    = 182206791;
    private const int FinalItem    = 182206792;

    private readonly IItemDao _itemDao;

    public _11116MunchingMookiePickles(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Relay1Npc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Relay2Npc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (targetId == StartNpc && (entry is null || entry.Status == QuestStatus.NONE))
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry is null) return false;

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
                if (dialog == DialogAction.SELECT_QUEST_REWARD)
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }
        else if (entry.Status != QuestStatus.START)
        {
            return false;
        }

        int var = entry.GetVar(0);
        if (targetId == Relay1Npc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 0)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog == DialogAction.SETPRO1 && var == 0)
                return await DefaultCloseDialogAsync(env, conn, 0, 1, reward: false, sameNpc: false, ct);
        }
        else if (targetId == Relay2Npc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 1)
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM && var == 1)
            {
                long count = player.Inventory.FindByItemId(CollectItem)?.Count ?? 0;
                if (count >= 10)
                {
                    await RemoveQuestItemAsync(player, conn, _itemDao, CollectItem, 10, ct);
                    if (await GiveQuestItemAsync(player, conn, _itemDao, TokenItem, 1, ct))
                        entry.SetVar(0, var + 1);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 10000, ct);
                }
                return await SendQuestDialogAsync(conn, targetObjId, 10001, ct);
            }
        }
        else if (targetId == TurnInNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 2)
                return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
            if (dialog == DialogAction.SET_SUCCEED && var == 2)
            {
                if (await GiveQuestItemAsync(player, conn, _itemDao, FinalItem, 1, ct))
                    entry.SetVar(0, var + 1);
                entry.Status = QuestStatus.REWARD;
                await UpdateQuestStatusAsync(conn, entry, ct);
                return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
            }
        }
        return false;
    }
}
