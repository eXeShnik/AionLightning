// Port of Java data/scripts/system/handlers/quest/miragent_holy_templar/_3936DecorationsOfSanctum.java
// (Nanou, modified by Gigi). Single-npc quest at Dairos (203710): accept, then hand in 10x each of
// four decoration items (182206091/092/093/094) to flip straight to reward; repeatable (NONE and
// COMPLETE both re-show the start dialog, matching Java's status guard).
// Java's QUEST_SELECT case falls through (no break) into SETPRO1 when var isn't 0 or 1 — reproduced
// with an explicit `goto case` (SETPRO1's own var==0 gate then makes it a no-op in that situation).
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

namespace Quest.MiragentHolyTemplar;

public sealed class _3936DecorationsOfSanctum : QuestHandlerBase
{
    private const int QuestIdConst = 3936;
    private const int DairosNpc    = 203710;
    private const int Decoration1  = 182206091;
    private const int Decoration2  = 182206092;
    private const int Decoration3  = 182206093;
    private const int Decoration4  = 182206094;
    private const int RequiredEach = 10;

    private readonly IItemDao _itemDao;

    public _3936DecorationsOfSanctum(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(DairosNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(DairosNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (targetId != DairosNpc) return false;

        if (entry is null || entry.Status is QuestStatus.NONE or QuestStatus.COMPLETE)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT:
                    if (var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                    if (var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    goto case DialogAction.SETPRO1;
                case DialogAction.SETPRO1:
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                case DialogAction.CHECK_USER_HAS_QUEST_ITEM:
                    if (HasItem(player, Decoration1, RequiredEach) && HasItem(player, Decoration2, RequiredEach)
                        && HasItem(player, Decoration3, RequiredEach) && HasItem(player, Decoration4, RequiredEach))
                    {
                        await RemoveQuestItemAsync(player, conn, _itemDao, Decoration1, RequiredEach, ct);
                        await RemoveQuestItemAsync(player, conn, _itemDao, Decoration2, RequiredEach, ct);
                        await RemoveQuestItemAsync(player, conn, _itemDao, Decoration3, RequiredEach, ct);
                        await RemoveQuestItemAsync(player, conn, _itemDao, Decoration4, RequiredEach, ct);
                        await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: true, ct);
                        return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                    }
                    return await SendQuestDialogAsync(conn, targetObjId, 10001, ct);
                default:
                    return false;
            }
        }

        if (entry.Status == QuestStatus.REWARD)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }

    private static bool HasItem(Player player, int itemId, long count)
    {
        var item = player.Inventory.FindByItemId(itemId);
        return item is not null && item.Count >= count;
    }
}
