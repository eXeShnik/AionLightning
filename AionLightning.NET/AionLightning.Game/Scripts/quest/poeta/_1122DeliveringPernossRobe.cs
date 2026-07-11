// Port of Java data/scripts/system/handlers/quest/poeta/_1122DeliveringPernossRobe.java.
// Take Pernos's Note (182200216) from Tapaya (203060); deliver to Pernos (790001), who accepts
// one of three robes (182200218/219/220 → var 1/2/3) and picks the matching reward tier.
using System.Linq;
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

namespace Quest.Poeta;

public sealed class _1122DeliveringPernossRobe : QuestHandlerBase
{
    private const int QuestIdConst = 1122;
    private const int TapayaNpc    = 203060;
    private const int PernosNpc    = 790001;
    private const int NoteItemId   = 182200216;

    private readonly IItemDao _itemDao;

    public _1122DeliveringPernossRobe(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(TapayaNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(TapayaNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(PernosNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (targetId == TapayaNpc)
        {
            if (entry is null)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (env.DialogId == (int)DialogAction.QUEST_ACCEPT_1)
                {
                    if (await GiveQuestItemAsync(player, conn, _itemDao, NoteItemId, 1, ct))
                        return await SendQuestStartDialogAsync(env, conn, ct);
                    return true;
                }
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (targetId == PernosNpc && entry is not null)
        {
            if (entry.Status == QuestStatus.START)
            {
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT:
                        return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    case DialogAction.SETPRO1:
                        return await DeliverAsync(player, conn, entry, targetObjId, 182200218, 1, 1523, ct);
                    case DialogAction.SETPRO2:
                        return await DeliverAsync(player, conn, entry, targetObjId, 182200219, 2, 1438, ct);
                    case DialogAction.SETPRO3:
                        return await DeliverAsync(player, conn, entry, targetObjId, 182200220, 3, 1353, ct);
                    default:
                        return await SendQuestStartDialogAsync(env, conn, ct);
                }
            }

            if (entry.Status == QuestStatus.REWARD)
            {
                int var = entry.GetVar(0);
                if (dialog == DialogAction.USE_OBJECT || env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
                    return await SendQuestDialogAsync(conn, targetObjId, 4 + var, ct);
                if (env.DialogId == (int)DialogAction.SELECTED_QUEST_NOREWARD)
                {
                    await FinishQuestAsync(conn, player, var - 1, ct);
                    await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                    return true;
                }
            }
        }
        return false;
    }

    private async ValueTask<bool> DeliverAsync(Player player, GsClientConnection conn, QuestEntry entry,
        int targetObjId, int robeItemId, int var, int okDialogId, CancellationToken ct)
    {
        if ((player.Inventory.FindByItemId(robeItemId)?.Count ?? 0) <= 0)
            return await SendQuestDialogAsync(conn, targetObjId, 1608, ct);

        entry.SetVar(0, var);
        entry.Status = QuestStatus.REWARD;
        await UpdateQuestStatusAsync(conn, entry, ct);
        await RemoveQuestItemAsync(player, conn, _itemDao, robeItemId, 1, ct);
        await RemoveQuestItemAsync(player, conn, _itemDao, NoteItemId, 1, ct);
        return await SendQuestDialogAsync(conn, targetObjId, okDialogId, ct);
    }
}
