// Port of Java data/scripts/system/handlers/quest/brusthonin/_4078ALightThroughtheTrees.java.
// Talk to 205157 to start; hand in 9x branches (182209049) for a torch (182209050, var 0->1);
// light three braziers in sequence (700428 var 1->2, 700427 var 2->3, 700429 var 3 -> REWARD),
// each requiring the torch still in hand; turn in back at 205157.
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

namespace Quest.Brusthonin;

public sealed class _4078ALightThroughtheTrees : QuestHandlerBase
{
    private const int QuestIdConst  = 4078;
    private const int StartNpc      = 205157;
    private const int BrazierBObj   = 700428;
    private const int BrazierAObj   = 700427;
    private const int BrazierCObj   = 700429;
    private const int BranchItemId  = 182209049;
    private const int TorchItemId   = 182209050;

    private readonly IItemDao _itemDao;

    public _4078ALightThroughtheTrees(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(BrazierBObj).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(BrazierAObj).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(BrazierCObj).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (targetId == StartNpc)
        {
            var startEntry = player.Quests.Get(QuestId);
            if (startEntry is null)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
        }

        var entry = player.Quests.Get(QuestId);
        if (entry is null) return false;
        int var = entry.GetVar(0);

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
                if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
            return false;
        }
        if (entry.Status != QuestStatus.START) return false;

        if (targetId == StartNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 0)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (env.DialogId == (int)DialogAction.CHECK_USER_HAS_QUEST_ITEM && var == 0)
            {
                long branchCount = player.Inventory.FindByItemId(BranchItemId)?.Count ?? 0;
                if (branchCount >= 9)
                {
                    if (!await GiveQuestItemAsync(player, conn, _itemDao, TorchItemId, 1, ct))
                        return true;
                    await RemoveQuestItemAsync(player, conn, _itemDao, BranchItemId, 9, ct);
                    entry.SetVar(0, var + 1);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 10000, ct);
                }
                return await SendQuestDialogAsync(conn, targetObjId, 10001, ct);
            }
            return false;
        }

        long torchCount = player.Inventory.FindByItemId(TorchItemId)?.Count ?? 0;
        if (targetId == BrazierBObj)
        {
            if (dialog == DialogAction.USE_OBJECT && var == 1 && torchCount == 1)
                return await UseQuestObjectAsync(env, conn, 1, 2, false, false, ct);
            return false;
        }
        if (targetId == BrazierAObj)
        {
            if (dialog == DialogAction.USE_OBJECT && var == 2 && torchCount == 1)
                return await UseQuestObjectAsync(env, conn, 2, 3, false, false, ct);
            return false;
        }
        if (targetId == BrazierCObj)
        {
            if (dialog == DialogAction.USE_OBJECT && var == 3 && torchCount == 1)
                return await UseQuestObjectAsync(env, conn, 3, 4, true, false, ct);
            return false;
        }
        return false;
    }
}
