// Port of Java data/scripts/system/handlers/quest/rider_quests/_4542TheSecretOfTheSeirenTreasure.java (pralinka).
// Talk to Sleipnir (204768) to start (hands out a starter item); Rubelik (204743, var0 0->1 trading
// the starter item for a forge item, then 2->3, then the var0==6 collect-check turn-in path); back to
// Sleipnir (var0 1->2, removes the forge item + movie 239); Esnu (204808, var0 2->3 with movie 240,
// collect-item check 3->4, hands out the reward-token item 4->5); turn in at Sleipnir (var0==5) or
// back at Rubelik (var0==6, alternate reward path with movie 239). Java's registerOnLevelUp(questId)
// has no matching onLvlUpEvent override in the original - dropped here as a no-op registration.
// Java bugs fixed: every per-npc dialog switch here had no break after its QUEST_SELECT case(s), so
// talking with an unexpected var fell through into the next case's body:
//  - Rubelik: fell into SETPRO3's `defaultCloseDialog(2, 3)` unconditionally (would erroneously
//    advance var0 2->3 on a plain QUEST_SELECT if var0 happened to be 2).
//  - Sleipnir: fell into SETPRO2's body, which removed the forge item and played movie 239 *before*
//    defaultCloseDialog's own step guard - a real duplicate-removal/movie-replay bug.
//  - Esnu: fell into the CHECK_USER_HAS_QUEST_ITEM case's `checkQuestItems(...)` unconditionally,
//    which would consume the reward-token item and advance the step on a plain QUEST_SELECT if
//    var0 happened to be 3.
// All three are ported with explicit dialog-id checks (no physical fallthrough) so each side effect
// only fires on its own genuine dialog action.
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

namespace Quest.RiderQuests;

public sealed class _4542TheSecretOfTheSeirenTreasure : QuestHandlerBase
{
    private const int QuestIdConst = 4542;
    private const int SleipnirNpc = 204768;
    private const int RubelikNpc  = 204743;
    private const int EsnuNpc     = 204808;
    private const int StartItem       = 182215327;
    private const int ForgeItem       = 182215328;
    private const int RewardTokenItem = 182215330;

    private readonly IItemDao _itemDao;

    public _4542TheSecretOfTheSeirenTreasure(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(SleipnirNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(SleipnirNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(RubelikNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(EsnuNpc).OnTalk.Add(QuestId);
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
            if (targetId == SleipnirNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
                if (dialog == DialogAction.QUEST_ACCEPT_SIMPLE)
                {
                    await GiveQuestItemAsync(player, conn, _itemDao, StartItem, 1, ct);
                    return await SendQuestStartDialogAsync(env, conn, ct);
                }
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            int var0 = entry.GetVar(0);

            if (targetId == RubelikNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var0 == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.QUEST_SELECT && var0 == 6)
                {
                    var held = player.Inventory.FindByItemId(RewardTokenItem);
                    if (held is not null && held.Count >= 1) return await SendQuestDialogAsync(conn, targetObjId, 3057, ct);
                    return false;
                }
                if (dialog == DialogAction.SETPRO1 && var0 == 0)
                {
                    await GiveQuestItemAsync(player, conn, _itemDao, ForgeItem, 1, ct);
                    await RemoveQuestItemAsync(player, conn, _itemDao, StartItem, 1, ct);
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                }
                if (dialog == DialogAction.SETPRO3) return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
                if (dialog == DialogAction.SELECT_QUEST_REWARD)
                {
                    await RemoveQuestItemAsync(player, conn, _itemDao, RewardTokenItem, 1, ct);
                    return await DefaultCloseDialogAsync(env, conn, 6, 6, reward: true, sameNpc: true, ct);
                }
                if (dialog == DialogAction.SELECT_ACTION_3143) return await SendQuestDialogAsync(conn, targetObjId, 3143, ct);
                if (dialog == DialogAction.SETPRO7)
                {
                    await PlayQuestMovieAsync(conn, player, 239, ct);
                    await RemoveQuestItemAsync(player, conn, _itemDao, RewardTokenItem, 1, ct);
                    return await DefaultCloseDialogAsync(env, conn, 6, 6, reward: true, sameNpc: true, ct);
                }
                return false;
            }
            if (targetId == SleipnirNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var0 == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.QUEST_SELECT && var0 == 5) return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                if (dialog == DialogAction.SETPRO2)
                {
                    await RemoveQuestItemAsync(player, conn, _itemDao, ForgeItem, 1, ct);
                    await PlayQuestMovieAsync(conn, player, 239, ct);
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                }
                if (dialog == DialogAction.SELECT_QUEST_REWARD)
                {
                    await RemoveQuestItemAsync(player, conn, _itemDao, RewardTokenItem, 1, ct);
                    return await DefaultCloseDialogAsync(env, conn, 5, 5, reward: true, sameNpc: false, ct);
                }
                return false;
            }
            if (targetId == EsnuNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var0 == 2) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.QUEST_SELECT && var0 == 3) return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                if (dialog == DialogAction.QUEST_SELECT && var0 == 4) return await SendQuestDialogAsync(conn, targetObjId, 2376, ct);
                if (dialog == DialogAction.SETPRO3 && var0 == 2)
                {
                    await PlayQuestMovieAsync(conn, player, 240, ct);
                    return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
                }
                if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                    return await CheckQuestItemsAsync(env, conn, _itemDao, 3, 4, reward: false, checkOkId: 10000, checkFailId: 10001, ct);
                if (dialog == DialogAction.SETPRO5)
                    return await DefaultCloseDialogAsync(env, conn, _itemDao, 4, 5, reward: false, sameNpc: false,
                        giveItemId: RewardTokenItem, giveItemCount: 1, removeItemId: 0, removeItemCount: 0, ct);
                return false;
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == SleipnirNpc) return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }
}
