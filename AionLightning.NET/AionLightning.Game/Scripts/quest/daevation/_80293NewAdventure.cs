// Port of Java data/scripts/system/handlers/quest/daevation/_80293NewAdventure.java.
// Same "A New Choice"-style exchange flow as _1994ANewChoice, at NPC 831386, but gated on holding
// 20 Daevanion's Light (not 1) and consuming 20 at turn-in; 14 exchangeable items, 6 reward tiers.
// Java bug: the item/choice selections were held in per-handler-instance fields (`choice`, `item`)
// instead of per-player quest state -- since one handler instance is shared by every player running
// this quest, concurrent players would clobber each other's in-progress selection. Ported using the
// per-player QuestEntry vars instead (var1 = exchanged-item index, var2 = reward tier).
using System;
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

namespace Quest.Daevation;

public sealed class _80293NewAdventure : QuestHandlerBase
{
    private const int QuestIdConst        = 80293;
    private const int Npc                 = 831386;
    private const int DaevanionsLight     = 186000041;
    private const int DaevanionsLightCost = 20;

    private static readonly int[] Dialogs =
    [
        1013, 1034, 1055, 1076, 5103, 1098, 1119, 1140, 1161, 1183, 1204, 1225, 1246, 5105,
    ];

    private static readonly int[] Items =
    [
        100001449, 100901122, 101301058, 100201267, 101701157, 100101106, 101501139,
        100601194, 100501113, 115001477, 101801053, 101901003, 102001076, 102100953,
    ];

    private readonly IItemDao _itemDao;

    public _80293NewAdventure(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        var npc = engine.RegisterQuestNpc(Npc);
        npc.OnQuestStart.Add(QuestId);
        npc.OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        if (env.TargetId != Npc) return false;

        var player      = env.Player;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var entry       = player.Quests.Get(QuestId);
        int dialogId    = env.DialogId;

        // Java bug-workaround note: qs.canRepeat() (max_repeat_count="255") isn't ported -
        // approximated as "no active entry", same simplification used throughout this port.
        if (entry is null)
        {
            if (DialogActionLookup.FromId(dialogId) == DialogAction.EXCHANGE_COIN)
            {
                await StartMissionAsync(conn, player, QuestStatus.START, ct);
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            }
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            if (DialogActionLookup.FromId(dialogId) == DialogAction.EXCHANGE_COIN)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);

            switch (dialogId)
            {
                case 1012:
                case 1097:
                case 1182:
                    return await SendQuestDialogAsync(conn, targetObjId, dialogId, ct);
            }

            int dialogIndex = Array.IndexOf(Dialogs, dialogId);
            if (dialogIndex != -1)
            {
                var owned = player.Inventory.FindByItemId(Items[dialogIndex]);
                if ((owned?.Count ?? 0) > 0)
                {
                    entry.SetVar(1, dialogIndex);
                    return await SendQuestDialogAsync(conn, targetObjId, 1013, ct);
                }
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            }

            switch (dialogId)
            {
                case 10000:
                case 10001:
                case 10002:
                case 10003:
                {
                    if ((player.Inventory.FindByItemId(DaevanionsLight)?.Count ?? 0) < DaevanionsLightCost)
                        return await SendQuestDialogAsync(conn, targetObjId, 1009, ct);

                    int choice = dialogId - 10000;
                    entry.SetVar(2, choice);
                    await ChangeQuestStepAsync(conn, entry, 0, 0, toReward: true, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, choice + 5, ct);
                }

                case 10004:
                {
                    if ((player.Inventory.FindByItemId(DaevanionsLight)?.Count ?? 0) < DaevanionsLightCost)
                        return await SendQuestDialogAsync(conn, targetObjId, 1009, ct);

                    entry.SetVar(2, dialogId - 10000);
                    await ChangeQuestStepAsync(conn, entry, 0, 0, toReward: true, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 45, ct);
                }

                case 10005:
                {
                    if ((player.Inventory.FindByItemId(DaevanionsLight)?.Count ?? 0) < DaevanionsLightCost)
                        return await SendQuestDialogAsync(conn, targetObjId, 1009, ct);

                    entry.SetVar(2, dialogId - 10000);
                    await ChangeQuestStepAsync(conn, entry, 0, 0, toReward: true, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 46, ct);
                }
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            int itemIndex = entry.GetVar(1);
            int choice    = entry.GetVar(2);
            await RemoveQuestItemAsync(player, conn, _itemDao, Items[itemIndex], 1, ct);
            await RemoveQuestItemAsync(player, conn, _itemDao, DaevanionsLight, DaevanionsLightCost, ct);
            return await FinishQuestAsync(conn, player, choice, ct);
        }

        return false;
    }
}
