// Port of Java data/scripts/system/handlers/quest/reshanta/_1071SpeakingBalaur.java (Rhys2002).
// Talk to the start npc (278532, var 0->1), then a branching chain through 798026 (var 1->2 or a
// 20000-kinah shortcut straight to var 7), 798025 (var 2->3), 279019 (var 3->4, gives item
// 182202002), back to 798026 (var 4->5, swaps item 182202002 for 182202001), use item 182202001
// to advance (var+1), then 798026 again (var 6 or 8 -> REWARD). Zone-mission chain, level-up gated
// on quest 1701.
// Skip vs Java: the SM_ITEM_USAGE_ANIMATION broadcast on item use is a cosmetic nearby-player
// visual with no broadcast-to-nearby-players primitive available from OnItemUseAsync's signature
// (conn is the acting player's own connection) - omitted; the var/item state change still applies.
// Java bug fixed: the kinah shortcut decreases kinah BEFORE attempting to give the Fragment item,
// so a full bag silently consumes the player's 20000 kinah for nothing; this port gives the item
// first and only charges kinah on success.
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

namespace Quest.Reshanta;

public sealed class _1071SpeakingBalaur : QuestHandlerBase
{
    private const int QuestIdConst = 1071;
    private const int StartNpc     = 278532;
    private const int Npc798026    = 798026;
    private const int Npc798025    = 798025;
    private const int Npc279019    = 279019;
    private const int FragmentItem = 182202001;
    private const int TokenItem    = 182202002;
    private const int KinahItemId  = 182400001;

    private readonly IItemDao _itemDao;

    public _1071SpeakingBalaur(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestItem(FragmentItem, QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Npc798026).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Npc798025).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Npc279019).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 1701, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);
        int var         = entry.GetVar(0);

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId != StartNpc) return false;
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        if (entry.Status != QuestStatus.START) return false;

        if (targetId == StartNpc)
        {
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT when var == 0:
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                case DialogAction.SETPRO1 when var == 0:
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                default:
                    return false;
            }
        }

        if (targetId == Npc798026)
        {
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT when var == 1:
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                case DialogAction.QUEST_SELECT when var == 4:
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                case DialogAction.QUEST_SELECT when var == 6 || var == 8:
                    return await SendQuestDialogAsync(conn, targetObjId, 3057, ct);
                case DialogAction.SETPRO5 when var == 4:
                    return await DefaultCloseDialogAsync(env, conn, _itemDao, 4, 5, reward: false, sameNpc: false,
                        giveItemId: FragmentItem, giveItemCount: 1, removeItemId: TokenItem, removeItemCount: 1, ct);
                case DialogAction.SETPRO7 when var == 6 || var == 8:
                    await ChangeQuestStepAsync(conn, entry, -1, 0, toReward: true, ct);
                    return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                case DialogAction.SETPRO11 when var == 1:
                    return await TryKinahShortcutAsync(player, entry, conn, targetObjId, ct);
                case DialogAction.SETPRO12 when var == 1:
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                default:
                    return false;
            }
        }

        if (targetId == Npc798025)
        {
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT when var == 2:
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                case DialogAction.SETPRO3 when var == 2:
                    return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
                default:
                    return false;
            }
        }

        if (targetId == Npc279019)
        {
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT when var == 3:
                    return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                case DialogAction.SETPRO4 when var == 3:
                    return await DefaultCloseDialogAsync(env, conn, _itemDao, 3, 4, reward: false, sameNpc: false,
                        giveItemId: TokenItem, giveItemCount: 1, removeItemId: 0, removeItemCount: 0, ct);
                default:
                    return false;
            }
        }

        return false;
    }

    /// <summary>Pays 20000 kinah (Java's decreaseKinah check) to skip straight from var 1 to 7,
    /// handing over the Fragment item directly instead of walking the 798025/279019/798026 steps.</summary>
    private async ValueTask<bool> TryKinahShortcutAsync(Player player, QuestEntry entry, GsClientConnection conn, int targetObjId, CancellationToken ct)
    {
        var kinah = player.Inventory.FindByItemId(KinahItemId);
        if ((kinah?.Count ?? 0) < 20000)
            return await SendQuestDialogAsync(conn, targetObjId, 1355, ct);

        if (!await GiveQuestItemAsync(player, conn, _itemDao, FragmentItem, 1, ct))
            return true;

        await RemoveQuestItemAsync(player, conn, _itemDao, KinahItemId, 20000, ct);
        entry.SetVar(0, 7);
        await UpdateQuestStatusAsync(conn, entry, ct);
        return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != FragmentItem) return false;
        var entry = player.Quests.Get(QuestId);
        if (entry is null) return false;

        await RemoveQuestItemAsync(player, conn, _itemDao, FragmentItem, 1, ct);
        await ChangeQuestStepAsync(conn, entry, 0, entry.GetVar(0) + 1, toReward: false, ct);
        return true;
    }
}
