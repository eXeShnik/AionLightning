// Port of Java data/scripts/system/handlers/quest/pernon/_28828TheManyFacetsOfFriendship.java (Rolandas, bobobear).
// Talk to any of 5 butler-template npcs (810022-810026) to accept — the accept dialog gives item
// 182213205 x1 (Java's item-overload sendQuestStartDialog(env, itemId, count): aborts the accept if
// the bag can't fit it, same guard as oriel/_18828UserFriendly.cs); using the item while START flips
// straight to REWARD (var 0->1) via the item-use trigger; turn in at the butler, which removes the
// item on SELECT_QUEST_REWARD before showing the end dialog — Java's switch falls through from
// SELECT_QUEST_REWARD into SELECTED_QUEST_NOREWARD's body with no `break`, ported 1:1 via the
// explicit OR below.
// Skip vs Java (documented): the Java handler also requires `player.getActiveHouse() != null &&
// house.getButler().getNpcId() == targetId` (your OWN house's butler only) before any dialog. This
// port has no House/ActiveHouse/Butler model at all (see migration_plan.md housing gap), so that
// ownership gate is omitted; any of the 5 butler npc templates is accepted. This only widens who may
// interact with the butler dialog — no var/status transition changes.
using System.Collections.Generic;
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

namespace Quest.Pernon;

public sealed class _28828TheManyFacetsOfFriendship : QuestHandlerBase
{
    private const int QuestIdConst = 28828;
    private const int GiftItemId   = 182213205;

    private static readonly HashSet<int> Butlers = [810022, 810023, 810024, 810025, 810026];

    private readonly IItemDao _itemDao;

    public _28828TheManyFacetsOfFriendship(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        foreach (int butlerId in Butlers)
        {
            engine.RegisterQuestNpc(butlerId).OnQuestStart.Add(QuestId);
            engine.RegisterQuestNpc(butlerId).OnTalk.Add(QuestId);
        }
        engine.RegisterQuestItem(GiftItemId, QuestId);
    }

    // Java onItemUseEvent: using the gift while START flips straight to REWARD. Returns false
    // (Java HandlerResult.UNKNOWN) either way — this quest item has no other on-use behavior to
    // preempt, so normal item-use processing is left to continue.
    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != GiftItemId) return false;
        var entry = player.Quests.Get(QuestId);
        if (entry is { Status: QuestStatus.START })
            await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: true, ct);
        return false;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        int targetId = env.TargetId;
        if (!Butlers.Contains(targetId)) return false;

        var entry = player.Quests.Get(QuestId);
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog is DialogAction.QUEST_ACCEPT_1 or DialogAction.QUEST_ACCEPT_SIMPLE)
            {
                if (!await GiveQuestItemAsync(player, conn, _itemDao, GiftItemId, 1, ct)) return true;
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            if (dialog is DialogAction.SELECT_QUEST_REWARD or DialogAction.SELECTED_QUEST_NOREWARD)
            {
                if (dialog == DialogAction.SELECT_QUEST_REWARD)
                    await RemoveQuestItemAsync(player, conn, _itemDao, GiftItemId, 1, ct);
                await SendQuestEndDialogAsync(env, conn, ct);
                return true;
            }
            return false;
        }

        return false;
    }
}
