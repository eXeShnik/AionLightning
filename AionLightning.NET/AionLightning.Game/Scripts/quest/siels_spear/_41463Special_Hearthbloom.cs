// Port of Java data/scripts/system/handlers/quest/siels_spear/_41463Special_Hearthbloom.java (Cheatkiller).
// Talk to 205579 to start; at 205580, gives a Firewood item (170190066) via SETPRO1 (var 0->1);
// once carrying 7x Hearthbloom (182213224), CHECK_USER_HAS_QUEST_ITEM_SIMPLE consumes all 7 and
// flips straight to REWARD (var set to 2); turn in at 205580.
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

namespace Quest.SielsSpear;

public sealed class _41463Special_Hearthbloom : QuestHandlerBase
{
    private const int QuestIdConst    = 41463;
    private const int StartNpc        = 205579;
    private const int TurnInNpc       = 205580;
    private const int HearthbloomItem = 182213224;
    private const int FirewoodItem    = 170190066;
    private const int HearthbloomNeeded = 7;

    private readonly IItemDao _itemDao;

    public _41463Special_Hearthbloom(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId == StartNpc)
            {
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT:
                        return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                    case DialogAction.QUEST_ACCEPT_SIMPLE:
                        return await SendQuestStartDialogAsync(env, conn, ct);
                    case DialogAction.QUEST_REFUSE_SIMPLE:
                        return await SendQuestEndDialogAsync(env, conn, ct);
                }
            }
        }
        else if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);
            if (targetId == TurnInNpc)
            {
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT:
                    {
                        if (var == 0)
                            return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                        long count = player.Inventory.FindByItemId(HearthbloomItem)?.Count ?? 0;
                        if (count >= HearthbloomNeeded)
                            return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                        return await CloseDialogWindowAsync(conn, targetObjId, ct);
                    }
                    case DialogAction.CHECK_USER_HAS_QUEST_ITEM_SIMPLE:
                        await RemoveQuestItemAsync(player, conn, _itemDao, HearthbloomItem, HearthbloomNeeded, ct);
                        await ChangeQuestStepAsync(conn, entry, 0, 2, toReward: true, ct);
                        return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                    case DialogAction.SETPRO1:
                        if (await GiveQuestItemAsync(player, conn, _itemDao, FirewoodItem, 1, ct))
                            return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                        break;
                }
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == TurnInNpc)
                return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }
}
