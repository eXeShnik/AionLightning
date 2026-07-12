// Port of Java data/scripts/system/handlers/quest/sanctum/_1901KrallicPotion.java (edynamic90).
// Talk to Marmeia (203830) to start; branching chain through Kunberunerk (798026, var 0->1 costs
// 10000 kinah, then var 5->6 finale), Mapireck (798025, var 1->2, var 4->5 removes item 182206000),
// Maniparas (203131, var 2->3), Gaphyrk (798003, var 3->4, gives item 182206000); turn in at
// Marmeia (203864 is Ulaguru, the actual reward npc). Java bug fix: the original called
// updateQuestStatus (broadcasting SM_QUEST_ACTION) BEFORE flipping the in-memory status to REWARD
// at the Ulaguru turn-in, so the client briefly saw a stale START status; this port sets the
// status first so the broadcast is correct.
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

namespace Quest.Sanctum;

public sealed class _1901KrallicPotion : QuestHandlerBase
{
    private const int QuestIdConst  = 1901;
    private const int StartNpc      = 203830;
    private const int KunberunerkNpc = 798026;
    private const int MapireckNpc   = 798025;
    private const int ManiparasNpc  = 203131;
    private const int GaphyrkNpc    = 798003;
    private const int UlaguruNpc    = 203864;
    private const int StoneItemId   = 182206000;
    private const int KinahItemId   = 182400001;

    private readonly IItemDao _itemDao;

    public _1901KrallicPotion(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(KunberunerkNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(MapireckNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ManiparasNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(GaphyrkNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(UlaguruNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (targetId == StartNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (targetId == UlaguruNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            if (dialog == DialogAction.SELECT_QUEST_REWARD && entry is not null && entry.Status is not (QuestStatus.COMPLETE or QuestStatus.NONE))
            {
                entry.SetVar(0, 7);
                entry.Status = QuestStatus.REWARD;
                await UpdateQuestStatusAsync(conn, entry, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
            if (entry is { Status: QuestStatus.REWARD })
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 3398, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry is null || entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);

        if (targetId == KunberunerkNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT)
            {
                if (var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (var == 5) return await SendQuestDialogAsync(conn, targetObjId, 3057, ct);
            }
            if (dialog == DialogAction.SELECT_ACTION_1438)
            {
                if (await TryDecreaseKinahAsync(player, conn, 10000, ct))
                {
                    entry.SetVar(0, var + 1);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                    return true;
                }
                return await SendQuestDialogAsync(conn, targetObjId, 1523, ct);
            }
            if (dialog == DialogAction.SETPRO1 || dialog == DialogAction.SETPRO2)
            {
                entry.SetVar(0, var + 1);
                await UpdateQuestStatusAsync(conn, entry, ct);
                await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                return true;
            }
            if (dialog == DialogAction.SETPRO7)
            {
                entry.SetVar(0, var + 1);
                entry.Status = QuestStatus.REWARD;
                await UpdateQuestStatusAsync(conn, entry, ct);
                await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                return true;
            }
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (targetId == MapireckNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT)
            {
                if (var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (var == 4) return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
            }
            if (dialog == DialogAction.SETPRO3)
            {
                entry.SetVar(0, var + 1);
                await UpdateQuestStatusAsync(conn, entry, ct);
                await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                return true;
            }
            if (dialog == DialogAction.SETPRO6)
            {
                await RemoveQuestItemAsync(player, conn, _itemDao, StoneItemId, 1, ct);
                entry.SetVar(0, var + 1);
                await UpdateQuestStatusAsync(conn, entry, ct);
                await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                return true;
            }
            return false;
        }

        if (targetId == ManiparasNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
            if (dialog == DialogAction.SETPRO4)
            {
                entry.SetVar(0, var + 1);
                await UpdateQuestStatusAsync(conn, entry, ct);
                await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                return true;
            }
            return false;
        }

        if (targetId == GaphyrkNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            if (dialog == DialogAction.SETPRO5)
            {
                if ((player.Inventory.FindByItemId(StoneItemId)?.Count ?? 0) == 0)
                {
                    if (!await GiveQuestItemAsync(player, conn, _itemDao, StoneItemId, 1, ct)) return true;
                }
                entry.SetVar(0, var + 1);
                await UpdateQuestStatusAsync(conn, entry, ct);
                await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                return true;
            }
            return false;
        }

        return false;
    }

    private async ValueTask<bool> TryDecreaseKinahAsync(Player player, GsClientConnection conn, long amount, CancellationToken ct)
    {
        var kinah = player.Inventory.FindByItemId(KinahItemId);
        if ((kinah?.Count ?? 0) < amount) return false;
        return await RemoveQuestItemAsync(player, conn, _itemDao, KinahItemId, amount, ct);
    }
}
