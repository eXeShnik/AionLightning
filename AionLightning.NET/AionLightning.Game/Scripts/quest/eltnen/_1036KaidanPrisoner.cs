// Port of Java data/scripts/system/handlers/quest/eltnen/_1036KaidanPrisoner.java (Rhys2002, fixed Ritsu).
// Zone-mission quest, part of the Kaidan Fortress chain (1300): talk to Npc1 (203904, var 0->1),
// Npc2 (204045, var 1->2, movie 32), Npc3 (204003, var 2->3->4, movie 50, collect-item preview),
// Npc4 (204004, var 4->5, hands out the old evidence item), Npc5 (204020, var 5->6/REWARD, swaps
// the old evidence for the sealed one), then turn in at Telemachus (203901).
// Java bug fix: at Npc5, dialog SELECT_ACTION_2717 falls through (missing break) into the SETPRO5
// case body in the original - removing the old evidence item is only reachable by first triggering
// that fallthrough. Reproduced here with an explicit goto case (the one legitimate use of Java's
// fallthrough semantics in this file) rather than duplicating the SETPRO5 body.
// Skip vs Java: two TeleportService2.teleportTo calls (Npc2 SETPRO2, Npc4 SETPRO5) relocate the
// player mid-chain - no TeleportService2 exists in this port (same precedent as
// quest/eltnen/_1430ATeleportationExperiment.cs). The var/status transitions are kept.
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

namespace Quest.Eltnen;

public sealed class _1036KaidanPrisoner : QuestHandlerBase
{
    private const int QuestIdConst  = 1036;
    private const int Npc1          = 203904;
    private const int Npc2          = 204045;
    private const int Npc3          = 204003;
    private const int Npc4          = 204004;
    private const int Npc5          = 204020;
    private const int TelemachusNpc = 203901;

    private const int OldEvidenceItem = 182201004;
    private const int SealedEvidenceItem = 182201005;

    private readonly IItemDao _itemDao;

    public _1036KaidanPrisoner(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        foreach (int npc in new[] { Npc1, Npc2, Npc3, Npc4, Npc5, TelemachusNpc })
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 1300, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int var         = entry.GetVar(0);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == TelemachusNpc)
            {
                await RemoveQuestItemAsync(player, conn, _itemDao, SealedEvidenceItem, 1, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }
        else if (entry.Status != QuestStatus.START)
        {
            return false;
        }

        if (targetId == Npc1)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 0)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog == DialogAction.SETPRO1 && var == 0)
                return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
            return false;
        }

        if (targetId == Npc2)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 1)
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            if (dialog == DialogAction.SELECT_ACTION_1354 && var == 1)
            {
                await PlayQuestMovieAsync(conn, player, 32, ct);
                return false;
            }
            if (dialog == DialogAction.SETPRO2)
            {
                if (var != 1) return false;
                await ChangeQuestStepAsync(conn, entry, 0, 2, toReward: false, ct);
                return true;
            }
            return false;
        }

        if (targetId == Npc3)
        {
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT:
                    if (var == 2) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                    if (var == 3 && HasAllCollectItems(player)) return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 2120, ct);
                case DialogAction.SETPRO3:
                    return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
                case DialogAction.SETPRO4:
                    if (var != 3) return false;
                    await PlayQuestMovieAsync(conn, player, 50, ct);
                    return await DefaultCloseDialogAsync(env, conn, 3, 4, ct);
                default:
                    return false;
            }
        }

        if (targetId == Npc4)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 4)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            if (dialog == DialogAction.SETPRO5 && var == 4)
                return await DefaultCloseDialogAsync(env, conn, _itemDao, 4, 5, reward: false, sameNpc: false,
                    giveItemId: OldEvidenceItem, giveItemCount: 1, removeItemId: 0, removeItemCount: 0, ct);
            return false;
        }

        if (targetId == Npc5)
        {
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT when var == 5:
                    return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                case DialogAction.SELECT_ACTION_2717:
                    await RemoveQuestItemAsync(player, conn, _itemDao, OldEvidenceItem, 1, ct);
                    goto case DialogAction.SETPRO5;
                case DialogAction.SETPRO5:
                    if (var != 5) return false;
                    if (!await GiveQuestItemAsync(player, conn, _itemDao, SealedEvidenceItem, 1, ct)) return true;
                    await ChangeQuestStepAsync(conn, entry, 0, 6, toReward: true, ct);
                    return true;
                default:
                    return false;
            }
        }

        return false;
    }

    private bool HasAllCollectItems(Player player)
    {
        var items = Template?.CollectItems?.Items;
        if (items is not { Count: > 0 }) return false;
        foreach (var req in items)
        {
            var item = player.Inventory.FindByItemId(req.ItemId);
            if (item is null || item.Count < req.Count) return false;
        }
        return true;
    }
}
