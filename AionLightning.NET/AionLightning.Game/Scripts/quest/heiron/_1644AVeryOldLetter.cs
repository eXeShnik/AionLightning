// Port of Java data/scripts/system/handlers/quest/heiron/_1644AVeryOldLetter.java.
// Using the old letter (182201765) offers to start the quest (dialog 4, accepted via the
// item-less QUEST_ACCEPT_1 action); 204545 advances var0->1, 204537 consumes the letter (var1->2),
// 204545 again flips straight to REWARD; turn in at 204546. Using the letter again after
// completion just consumes it silently.
// Skip vs Java: the item-use animation broadcast + 3s delay before showing dialog 4 isn't ported
// (no cast-time animation/scheduling infra) — the dialog is shown immediately instead.
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.Heiron;

public sealed class _1644AVeryOldLetter : QuestHandlerBase
{
    private const int QuestIdConst = 1644;
    private const int FirstNpc  = 204545;
    private const int SecondNpc = 204537;
    private const int EndNpc    = 204546;
    private const int LetterItem = 182201765;

    private readonly IItemDao _itemDao;

    public _1644AVeryOldLetter(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(FirstNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(SecondNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(EndNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestItem(LetterItem, QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var entry  = player.Quests.Get(QuestId);
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (targetId == 0 && dialog == DialogAction.QUEST_ACCEPT_1)
        {
            await StartMissionAsync(conn, player, QuestStatus.START, ct);
            await conn.SendAsync(new SM_DIALOG_WINDOW(0, 0), ct);
            return true;
        }

        if (entry is not null && entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);
            if (targetId == FirstNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    if (var == 2) return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                    return false;
                }
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                if (dialog == DialogAction.SETPRO3)
                {
                    entry.SetVar(0, 3);
                    return await DefaultCloseDialogAsync(env, conn, 3, 3, reward: true, sameNpc: false, ct);
                }
            }
            else if (targetId == SecondNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SETPRO2)
                    return await DefaultCloseDialogAsync(env, conn, _itemDao, 1, 2, reward: false, sameNpc: false,
                        giveItemId: 0, giveItemCount: 0, removeItemId: LetterItem, removeItemCount: 1, ct);
            }
        }
        else if (entry is not null && entry.Status == QuestStatus.REWARD)
        {
            if (targetId == EndNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }
        return false;
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != LetterItem) return false;
        var entry = player.Quests.Get(QuestId);

        if (entry is not null && entry.Status == QuestStatus.COMPLETE)
        {
            await RemoveQuestItemAsync(player, conn, _itemDao, LetterItem, 1, ct);
            return true;
        }

        return await SendQuestDialogAsync(conn, 0, 4, ct);
    }
}
