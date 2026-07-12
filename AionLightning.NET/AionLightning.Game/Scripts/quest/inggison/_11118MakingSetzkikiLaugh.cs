// Port of Java data/scripts/system/handlers/quest/inggison/_11118MakingSetzkikiLaugh.java.
// Talk to 798985 to start (no item); relay at 798963 (var 0->1, then a collect-check for 20x
// item 182206794 -> gives 182206795); turn-in trigger at 798984 (var 2) flips to reward; turn in
// back at 798985. Java's switch-fallthrough between the SETPRO1/CHECK_USER_HAS_QUEST_ITEM cases at
// 798963 is collapsed to explicit per-dialog checks here (the fallthrough is only reachable if the
// client sent SETPRO1 while var==1, which the dialog UI never does — same page ids drive both).
// Java bug fixed: the collect-check advanced the var only when giveQuestItem FAILED
// (`if (!giveQuestItem(...)) var++`), stranding the quest forever on the common success path;
// ported as advance-on-success like every sibling give/remove step in this file.
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

public sealed class _11118MakingSetzkikiLaugh : QuestHandlerBase
{
    private const int QuestIdConst = 11118;
    private const int StartNpc     = 798985;
    private const int RelayNpc     = 798963;
    private const int TurnInNpc    = 798984;
    private const int CollectItem  = 182206794;
    private const int TokenItem    = 182206795;

    private readonly IItemDao _itemDao;

    public _11118MakingSetzkikiLaugh(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(RelayNpc).OnTalk.Add(QuestId);
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

        if (targetId == RelayNpc)
        {
            int var = entry.GetVar(0);
            if (dialog == DialogAction.QUEST_SELECT)
            {
                if (var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                return false;
            }
            if (dialog == DialogAction.SETPRO1 && var == 0)
                return await DefaultCloseDialogAsync(env, conn, 0, 1, reward: false, sameNpc: false, ct);
            if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM && var == 1)
            {
                long count = player.Inventory.FindByItemId(CollectItem)?.Count ?? 0;
                if (count >= 20)
                {
                    await RemoveQuestItemAsync(player, conn, _itemDao, CollectItem, 20, ct);
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
            if (dialog == DialogAction.QUEST_SELECT && entry.GetVar(0) == 2)
                return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
            if (dialog == DialogAction.SET_SUCCEED && entry.GetVar(0) == 2)
                return await DefaultCloseDialogAsync(env, conn, 2, 3, reward: true, sameNpc: false, ct);
        }
        return false;
    }
}
