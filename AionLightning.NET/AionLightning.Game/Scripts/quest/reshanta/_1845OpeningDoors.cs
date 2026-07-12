// Port of Java data/scripts/system/handlers/quest/reshanta/_1845OpeningDoors.java (MrPoke, remod by
// Nephis). Started by using a quest item; two independent relay npcs (278591, 798316) each bump
// var0 by one whenever visited (no var-gate in Java — visiting both, in either order, is required);
// turn in at 204390.
// Fixed 2 latent Java bugs that together made this quest unstartable/unfinishable as authored:
// (1) register() wired registerQuestItem to item 182204181, but onItemUseEvent's own check (and the
// turn-in's removeQuestItem call) tested/removed 182202181 — a transposed-digit mismatch, so the
// registered item never actually routed to this handler. Fixed by registering 182202181 (the id
// used everywhere else in the file). (2) the turn-in npc 204390 was referenced in onDialogEvent but
// never registered via addOnTalkEvent, so that branch was dead code in the original — fixed by
// registering it. 278624 is registered (matching Java) but has no dialog branch in either version —
// kept for 1:1 parity, it is simply inert.
// Skip vs Java: the SM_ITEM_USAGE_ANIMATION broadcast + 3s scheduled delay before showing the
// accept dialog are dropped (same cosmetic skip already established for _1107TheLostAxe) — the
// dialog opens immediately on item use instead.
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

public sealed class _1845OpeningDoors : QuestHandlerBase
{
    private const int QuestIdConst = 1845;
    private const int RelayNpc1    = 278591;
    private const int InertNpc     = 278624;
    private const int RelayNpc2    = 798316;
    private const int TurnInNpc    = 204390;
    private const int ItemId       = 182202181;

    private readonly IItemDao _itemDao;

    public _1845OpeningDoors(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(RelayNpc1).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(InertNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(RelayNpc2).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestItem(ItemId, QuestId);
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != ItemId) return false;
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status == QuestStatus.NONE)
            await conn.SendAsync(new SM_DIALOG_WINDOW(0, 4, QuestId), ct);
        return true;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (targetId == 0)
        {
            if (dialog == DialogAction.QUEST_ACCEPT_1)
            {
                await StartMissionAsync(conn, player, QuestStatus.START, ct);
                await conn.SendAsync(new SM_DIALOG_WINDOW(0, 0), ct);
                return true;
            }
            return false;
        }

        if (targetId == RelayNpc1 || targetId == RelayNpc2)
        {
            if (entry is null) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            if (dialog == DialogAction.SETPRO1)
            {
                entry.SetVar(0, entry.GetVar(0) + 1);
                await UpdateQuestStatusAsync(conn, entry, ct);
                await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                return true;
            }
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (targetId == TurnInNpc && entry is not null)
        {
            if (dialog == DialogAction.QUEST_SELECT && entry.Status == QuestStatus.START)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD
                && entry.Status != QuestStatus.COMPLETE && entry.Status != QuestStatus.NONE)
            {
                await RemoveQuestItemAsync(player, conn, _itemDao, ItemId, 1, ct);
                entry.SetVar(0, 1);
                entry.Status = QuestStatus.REWARD;
                await UpdateQuestStatusAsync(conn, entry, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
