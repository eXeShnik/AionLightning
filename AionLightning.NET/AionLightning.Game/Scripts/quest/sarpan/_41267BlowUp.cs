// Port of Java data/scripts/system/handlers/quest/sarpan/_41267BlowUp.java (Cheatkiller).
// Talk to 205762 to start (gives item 182213109); use the item (var 0->1, item consumed); return
// to 205762 (var==1, SELECT_QUEST_REWARD flips to REWARD, same npc); turn in at 205762.
// Skip vs Java: useQuestItem's 3s SM_ITEM_USAGE_ANIMATION delay before the step transition is
// applied immediately instead (cosmetic only, same simplification as other item-use quests in
// this port, e.g. altgard/_2216MuMuGrassKnot.cs).
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

namespace Quest.Sarpan;

public sealed class _41267BlowUp : QuestHandlerBase
{
    private const int QuestIdConst = 41267;
    private const int StartNpc      = 205762;
    private const int ItemId        = 182213109;

    private readonly IItemDao _itemDao;

    public _41267BlowUp(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestItem(ItemId, QuestId);
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != ItemId) return false;
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 0) return false;

        await RemoveQuestItemAsync(player, conn, _itemDao, ItemId, 1, ct);
        await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: false, ct);
        return true;
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
            if (targetId != StartNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            return await StartWithItemAsync(player, conn, targetObjId, dialog, ct);
        }

        if (entry.Status == QuestStatus.START && targetId == StartNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && entry.GetVar(0) == 1)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            if (dialog == DialogAction.SELECT_QUEST_REWARD)
                return await DefaultCloseDialogAsync(env, conn, 1, 1, reward: true, sameNpc: true, ct);
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == StartNpc)
        {
            if (dialog == DialogAction.USE_OBJECT) return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }

    private async ValueTask<bool> StartWithItemAsync(Player player, GsClientConnection conn, int targetObjId, DialogAction dialog, CancellationToken ct)
    {
        switch (dialog)
        {
            case DialogAction.QUEST_ACCEPT:
            case DialogAction.QUEST_ACCEPT_1:
                await StartMissionAsync(conn, player, QuestStatus.START, ct);
                await GiveQuestItemAsync(player, conn, _itemDao, ItemId, 1, ct);
                return await SendQuestDialogAsync(conn, targetObjId, 1003, ct);
            case DialogAction.QUEST_ACCEPT_SIMPLE:
                await StartMissionAsync(conn, player, QuestStatus.START, ct);
                await GiveQuestItemAsync(player, conn, _itemDao, ItemId, 1, ct);
                return await CloseDialogWindowAsync(conn, targetObjId, ct);
            case DialogAction.QUEST_REFUSE_1:
            case DialogAction.QUEST_REFUSE_2:
                return await SendQuestDialogAsync(conn, targetObjId, 1004, ct);
            case DialogAction.QUEST_REFUSE_SIMPLE:
                return await CloseDialogWindowAsync(conn, targetObjId, ct);
            default:
                return false;
        }
    }
}
