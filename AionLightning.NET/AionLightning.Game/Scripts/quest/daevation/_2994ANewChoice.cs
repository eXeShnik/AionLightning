// Port of Java data/scripts/system/handlers/quest/daevation/_2994ANewChoice.java
// (Asmodian mirror of _1994ANewChoice, same item/dialog tables, NPC Bor 204077).
// Java already stored the item/reward-tier choice per-player via qs.setReward/getReward (a packed
// int, not an instance field), so there is no shared-mutable bug here, but QuestEntry in this port
// has no equivalent field -- ported using its per-player vars instead (var1 = exchanged-item index,
// var2 = reward tier), which is a straight behavioral match.
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

public sealed class _2994ANewChoice : QuestHandlerBase
{
    private const int QuestIdConst    = 2994;
    private const int Bor             = 204077;
    private const int DaevanionsLight = 186000041;

    private static readonly int[] Dialogs =
    [
        1013, 1034, 1055, 1076, 5103, 1098, 1119, 1140, 1161, 1183, 1204, 1225, 1246,
    ];

    private static readonly int[] Items =
    [
        100000723, 100900554, 101300538, 100200673, 101700594, 100100568, 101500566,
        100600608, 100500572, 115000826, 101800569, 101900562, 102000592,
    ];

    private readonly IItemDao _itemDao;

    public _2994ANewChoice(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        var npc = engine.RegisterQuestNpc(Bor);
        npc.OnQuestStart.Add(QuestId);
        npc.OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        if (env.TargetId != Bor) return false;

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
                case 1012:
                case 1097:
                case 1182:
                case 1267:
                    return await SendQuestDialogAsync(conn, targetObjId, dialogId, ct);

                case 10000:
                case 10001:
                case 10002:
                case 10003:
                {
                    if ((player.Inventory.FindByItemId(DaevanionsLight)?.Count ?? 0) == 0)
                        return await SendQuestDialogAsync(conn, targetObjId, 1009, ct);

                    int choice = dialogId - 10000;
                    entry.SetVar(2, choice);
                    await ChangeQuestStepAsync(conn, entry, 0, 0, toReward: true, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, choice + 5, ct);
                }

                case 10004:
                case 10005:
                {
                    if ((player.Inventory.FindByItemId(DaevanionsLight)?.Count ?? 0) == 0)
                        return await SendQuestDialogAsync(conn, targetObjId, 1009, ct);

                    int choice = dialogId - 10000;
                    entry.SetVar(2, choice);
                    await ChangeQuestStepAsync(conn, entry, 0, 0, toReward: true, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, choice + 41, ct);
                }
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            int itemIndex = entry.GetVar(1);
            int choice    = entry.GetVar(2);
            await RemoveQuestItemAsync(player, conn, _itemDao, Items[itemIndex], 1, ct);
            await RemoveQuestItemAsync(player, conn, _itemDao, DaevanionsLight, 1, ct);
            return await FinishQuestAsync(conn, player, choice, ct);
        }

        return false;
    }
}
