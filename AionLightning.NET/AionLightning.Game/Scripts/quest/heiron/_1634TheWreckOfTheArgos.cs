// Port of Java data/scripts/system/handlers/quest/heiron/_1634TheWreckOfTheArgos.java.
// Talk to 204547 while carrying 3x item 182201760 (var0->1), then 204540 (var1->2, consumes one),
// then 790018 (var2->3, consumes one, flips to REWARD); turn in at 204541 (a leftover item is
// cleaned up if the player declines the reward).
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

namespace Quest.Heiron;

public sealed class _1634TheWreckOfTheArgos : QuestHandlerBase
{
    private const int QuestIdConst = 1634;
    private const int FirstNpc  = 204547;
    private const int SecondNpc = 204540;
    private const int ThirdNpc  = 790018;
    private const int EndNpc    = 204541;
    private const int WreckageItem = 182201760;

    private readonly IItemDao _itemDao;

    public _1634TheWreckOfTheArgos(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(FirstNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(FirstNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(SecondNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ThirdNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(EndNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var entry  = player.Quests.Get(QuestId);
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId == FirstNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);
            if (targetId == FirstNpc)
            {
                bool hasWreckage = player.Inventory.FindByItemId(WreckageItem) is { Count: >= 3 };
                if (dialog == DialogAction.QUEST_SELECT && var == 0 && hasWreckage)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SELECT_ACTION_4763)
                {
                    entry.SetVar(0, var + 1);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 0), ct);
                    return true;
                }
            }
            else if (targetId == SecondNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SELECT_ACTION_1694)
                {
                    entry.SetVar(0, var + 1);
                    await RemoveQuestItemAsync(player, conn, _itemDao, WreckageItem, 1, ct);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 0), ct);
                    return true;
                }
            }
            else if (targetId == ThirdNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                if (dialog == DialogAction.SELECT_ACTION_2035)
                {
                    entry.SetVar(0, var + 1);
                    await RemoveQuestItemAsync(player, conn, _itemDao, WreckageItem, 1, ct);
                    entry.Status = QuestStatus.REWARD;
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 0), ct);
                    return true;
                }
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == EndNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.SELECT_QUEST_REWARD)
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                if (dialog == DialogAction.SELECTED_QUEST_NOREWARD)
                    await RemoveQuestItemAsync(player, conn, _itemDao, WreckageItem, 1, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }
        return false;
    }
}
