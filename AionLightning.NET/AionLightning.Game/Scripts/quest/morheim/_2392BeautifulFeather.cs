// Port of Java data/scripts/system/handlers/quest/morheim/_2392BeautifulFeather.java.
// Start at 798085; SETPRO1/2/3 each check for a different feather item (182204159/60/61); the one
// the player is carrying is consumed, var flips to 1/2/3 and status to REWARD; turning in picks
// the reward tier matching that var (Java's sendQuestEndDialog(env, reward) two-step dialog).
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

namespace Quest.Morheim;

public sealed class _2392BeautifulFeather : QuestHandlerBase
{
    private const int QuestIdConst  = 2392;
    private const int StartNpc      = 798085;
    private const int FeatherItem1  = 182204159;
    private const int FeatherItem2  = 182204160;
    private const int FeatherItem3  = 182204161;

    private readonly IItemDao _itemDao;

    public _2392BeautifulFeather(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
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
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);

            (int itemId, int nextVar, int okDialog) = dialog switch
            {
                DialogAction.SETPRO1 => (FeatherItem1, 1, 5),
                DialogAction.SETPRO2 => (FeatherItem2, 2, 6),
                DialogAction.SETPRO3 => (FeatherItem3, 3, 7),
                _ => (0, 0, 0),
            };
            if (itemId == 0) return false;

            var carried = player.Inventory.FindByItemId(itemId);
            if (carried is null || carried.Count < 1)
                return await SendQuestDialogAsync(conn, targetObjId, 1097, ct);

            await RemoveQuestItemAsync(player, conn, _itemDao, itemId, 1, ct);
            await ChangeQuestStepAsync(conn, entry, 0, nextVar, toReward: true, ct);
            return await SendQuestDialogAsync(conn, targetObjId, okDialog, ct);
        }

        if (entry.Status == QuestStatus.REWARD && targetId == StartNpc)
        {
            int rewardIndex = entry.GetVar(0) switch { 2 => 1, 3 => 2, _ => 0 };
            return await SendQuestEndDialogWithRewardAsync(env, conn, rewardIndex, ct);
        }
        return false;
    }

    /// <summary>Java QuestHandler.sendQuestEndDialog(env, reward): SELECT_QUEST_REWARD/USE_OBJECT
    /// shows the tier-confirm page 5+reward; SELECTED_QUEST_REWARDn/NOREWARD actually completes
    /// with that reward tier.</summary>
    private async ValueTask<bool> SendQuestEndDialogWithRewardAsync(QuestEnv env, GsClientConnection conn, int rewardIndex, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.REWARD) return false;
        int targetObjId = env.Target?.ObjectId ?? 0;
        int dialogId = env.DialogId;

        if (dialogId >= (int)DialogAction.SELECTED_QUEST_REWARD1 && dialogId <= (int)DialogAction.SELECTED_QUEST_NOREWARD)
        {
            if (!await FinishQuestAsync(conn, env.Player, rewardIndex, ct)) return false;
            await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
            return true;
        }
        if (dialogId == (int)DialogAction.SELECT_QUEST_REWARD || dialogId == (int)DialogAction.USE_OBJECT)
            return await SendQuestDialogAsync(conn, targetObjId, 5 + rewardIndex, ct);
        return false;
    }
}
