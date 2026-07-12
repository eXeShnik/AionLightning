// Port of Java data/scripts/system/handlers/quest/fort_tiamat/_10073ShatteredAlliance.java (Cheatkiller).
// Hub 798600 (var0->1, gives item 182213245) -> item 182213245 used (OnItemUse, var1->2) -> 798600
// again (var2->3, gives items 182213246/182213247) -> 205579 (var3->5, removes both) -> 205987
// (var5->6) -> kill any of 4 beasts (218253/218333/218421/218464) while var0==6, tracking one flag
// var per beast (var1..4); once 3 distinct beasts are down, var0->7 -> 205579 (var7->8) -> 798600
// (var8->9, reward). Turn in at 790001.
// Java bug fixed: onKillEvent's beast-tracking list is a `private static List<Integer>` shared
// across all handler instances of this quest id (harmless in practice — populated once at
// register() and never mutated afterward — but flagged per the framework's mutable-shared-state
// convention); ported as a `static readonly int[]` instead.
// Ported as-is (not a guarded give): Java calls giveQuestItem for 182213245/246/247 without
// checking the return value, so a full inventory silently drops the item instead of blocking the
// step transition — kept faithful to that behavior rather than gating on GiveQuestItemAsync's result.
// Caveat: onLvlUpEvent gates this quest behind quest 10072 (_10072TheTruthRevealed), skipped in
// this port (needs InstanceService.getNextAvailableInstance, unavailable) — this handler is fully
// functional but currently unreachable via the level-up path.
using System;
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

namespace Quest.FortTiamat;

public sealed class _10073ShatteredAlliance : QuestHandlerBase
{
    private const int QuestIdConst = 10073;
    private const int HubNpc       = 798600;
    private const int MidNpc       = 205579;
    private const int ReportNpc    = 205987;
    private const int RewardNpc    = 790001;
    private const int TrackingItem = 182213245;
    private const int PieceItem1   = 182213246;
    private const int PieceItem2   = 182213247;

    private static readonly int[] Beasts = [218253, 218333, 218421, 218464];

    private readonly IItemDao _itemDao;

    public _10073ShatteredAlliance(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(HubNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(MidNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ReportNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(RewardNpc).OnTalk.Add(QuestId);
        foreach (int beast in Beasts)
            engine.RegisterQuestNpc(beast).OnKill.Add(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterOnEnterWorld(QuestId);
        engine.RegisterQuestItem(TrackingItem, QuestId);
    }

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 10072, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId != RewardNpc) return false;
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        if (entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);

        if (targetId == HubNpc)
        {
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT when var == 0:
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                case DialogAction.QUEST_SELECT when var == 2:
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                case DialogAction.QUEST_SELECT when var == 8:
                    return await SendQuestDialogAsync(conn, targetObjId, 4080, ct);
                case DialogAction.SETPRO1 when var == 0:
                    await GiveQuestItemAsync(player, conn, _itemDao, TrackingItem, 1, ct);
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                case DialogAction.SETPRO3 when var == 2:
                    await GiveQuestItemAsync(player, conn, _itemDao, PieceItem1, 1, ct);
                    await GiveQuestItemAsync(player, conn, _itemDao, PieceItem2, 1, ct);
                    return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
                case DialogAction.SET_SUCCEED when var == 8:
                    return await DefaultCloseDialogAsync(env, conn, 8, 9, reward: true, sameNpc: false, ct);
                default:
                    return false;
            }
        }

        if (targetId == MidNpc)
        {
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT when var == 3:
                    return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                case DialogAction.QUEST_SELECT when var == 7:
                    return await SendQuestDialogAsync(conn, targetObjId, 3398, ct);
                case DialogAction.SETPRO5 when var == 3:
                    await RemoveQuestItemAsync(player, conn, _itemDao, PieceItem1, 1, ct);
                    await RemoveQuestItemAsync(player, conn, _itemDao, PieceItem2, 1, ct);
                    return await DefaultCloseDialogAsync(env, conn, 3, 5, ct);
                case DialogAction.SETPRO8 when var == 7:
                    return await DefaultCloseDialogAsync(env, conn, 7, 8, ct);
                default:
                    return false;
            }
        }

        if (targetId == ReportNpc)
        {
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT when var == 5:
                    return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                case DialogAction.SETPRO6 when var == 5:
                    return await DefaultCloseDialogAsync(env, conn, 5, 6, ct);
                default:
                    return false;
            }
        }

        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (entry.GetVar(0) != 6) return false;

        int beastIdx = Array.IndexOf(Beasts, env.TargetId);
        if (beastIdx < 0) return false;

        int sum = entry.GetVar(1) + entry.GetVar(2) + entry.GetVar(3) + entry.GetVar(4);
        if (sum == 3)
        {
            await ChangeQuestStepAsync(conn, entry, 0, 7, toReward: false, ct);
            return true;
        }

        int flagVar = beastIdx + 1;
        if (entry.GetVar(flagVar) != 0) return false;

        await ChangeQuestStepAsync(conn, entry, flagVar, 1, toReward: false, ct);
        return true;
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != TrackingItem) return false;
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        await RemoveQuestItemAsync(player, conn, _itemDao, TrackingItem, 1, ct);
        await ChangeQuestStepAsync(conn, entry, 0, 2, toReward: false, ct);
        return true;
    }
}
