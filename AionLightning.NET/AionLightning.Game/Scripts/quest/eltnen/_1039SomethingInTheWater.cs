// Port of Java data/scripts/system/handlers/quest/eltnen/_1039SomethingInTheWater.java (Xitanium).
// Zone-mission quest, part of the Kaidan Fortress chain (1300): Asclepius (203946) starts it and
// hands out the collection item (182201009, var 0->1); Jumentis (203705) checks var 2 and removes
// item 182201010 (obtained via item-use in a fixed zone) to reach var 3; at var 3, using the
// collection item inside "LF2_ITEMUSEAREA_Q1039" grants item 182201010 (var 3->4 via
// GiveQuestItemAsync's UseQuestObjectAsync analogue); at var 4, two independent mob-kill counters
// (var slots 1 and 2, each capped at 3) must both fill before Asclepius can flip the quest to REWARD.
// Deviation: Java's defaultOnKillEvent(npcIds, startVar, endVar, varIdx) overload writes an
// arbitrary var slot; this port's DefaultOnKillEventAsync always targets var 0, so the two
// independent counters are bumped manually here instead.
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

public sealed class _1039SomethingInTheWater : QuestHandlerBase
{
    private const int QuestIdConst  = 1039;
    private const int AsclepiusNpc  = 203946;
    private const int JumentisNpc   = 203705;
    private const int CollectItem   = 182201009;
    private const int VaegirCatchItem = 182201010;
    private const string ItemUseZone = "LF2_ITEMUSEAREA_Q1039";

    private static readonly int[] _vaegirMobs  = [210946, 210968];
    private static readonly int[] _fighterMobs = [210969, 210947];

    private readonly IItemDao _itemDao;

    public _1039SomethingInTheWater(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestItem(CollectItem, QuestId);
        engine.RegisterQuestNpc(AsclepiusNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(JumentisNpc).OnTalk.Add(QuestId);
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        foreach (int mob in _vaegirMobs) engine.RegisterQuestNpc(mob).OnKill.Add(QuestId);
        foreach (int mob in _fighterMobs) engine.RegisterQuestNpc(mob).OnKill.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 1300, isZoneMission: true, ct);

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != CollectItem) return false;
        if (!player.CurrentZones.Contains(ItemUseZone)) return false;

        var env = new QuestEnv(null, player, QuestId, 0);
        return await UseQuestObjectAsync(env, conn, step: 1, nextStep: 2, reward: false, varNum: 0,
            addItemId: VaegirCatchItem, addItemCount: 1, removeItemId: 0, removeItemCount: 0,
            movieId: 0, dieObject: false, _itemDao, ct);
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 4) return false;

        int targetId = env.TargetId;
        if (targetId is 210946 or 210968)
            return await BumpKillCounterAsync(conn, entry, varIdx: 1, ct);
        if (targetId is 210969 or 210947)
            return await BumpKillCounterAsync(conn, entry, varIdx: 2, ct);
        return false;
    }

    private async ValueTask<bool> BumpKillCounterAsync(GsClientConnection conn, QuestEntry entry, int varIdx, CancellationToken ct)
    {
        int count = entry.GetVar(varIdx);
        if (count >= 3) return false;

        await ChangeQuestStepAsync(conn, entry, varIdx, count + 1, toReward: false, ct);
        return true;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.REWARD)
            return targetId == AsclepiusNpc && await SendQuestEndDialogAsync(env, conn, ct);
        if (entry.Status != QuestStatus.START) return false;

        int var  = entry.GetVar(0);
        int var1 = entry.GetVar(1);
        int var2 = entry.GetVar(2);

        if (targetId == AsclepiusNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT)
            {
                if (var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (var == 3) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (var == 4 && var1 == 3 && var2 == 3) return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                return false;
            }
            if (dialog == DialogAction.SETPRO1)
                return await DefaultCloseDialogAsync(env, conn, _itemDao, 0, 1, reward: false, sameNpc: false,
                    giveItemId: CollectItem, giveItemCount: 1, removeItemId: 0, removeItemCount: 0, ct);
            if (dialog == DialogAction.SETPRO3)
                return await DefaultCloseDialogAsync(env, conn, 3, 4, ct);
            if (dialog == DialogAction.SELECT_QUEST_REWARD)
            {
                await ChangeQuestStepAsync(conn, entry, varIdx: -1, newValue: 0, toReward: true, ct);
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            }
            return false;
        }

        if (targetId == JumentisNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 2)
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            if (dialog == DialogAction.SETPRO2)
                return await DefaultCloseDialogAsync(env, conn, _itemDao, 2, 3, reward: false, sameNpc: false,
                    giveItemId: 0, giveItemCount: 0, removeItemId: VaegirCatchItem, removeItemCount: 1, ct);
            return false;
        }

        return false;
    }
}
