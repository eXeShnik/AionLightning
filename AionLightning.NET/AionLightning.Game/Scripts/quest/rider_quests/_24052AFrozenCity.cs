// Port of Java data/scripts/system/handlers/quest/rider_quests/_24052AFrozenCity.java (pralinka).
// Zone-mission sub-quest of 24040: talk to the Frozen City npc (204753, var0 0->1, hands out 3
// evidence items), use any of them inside the DF3_ITEMUSEAREA_Q2056 zone to progress var0 1->2->3->4
// (movies 243/244/245, the last spawning a Kalu'ak boss + starting a 180s quest timer that rolls the
// step back to 0 if it expires while still at var0==4), kill the boss (var0 4 -> REWARD); turn in at
// the same npc (all 3 evidence items removed regardless of remaining count).
// Java bug: onDialogEvent's switch on the Frozen City npc had no break after the QUEST_SELECT case,
// so talking with var0!=0 fell through into the SELECT_ACTION_1012 body and played movie 242
// unconditionally. Fixed here so the movie only fires on an actual SELECT_ACTION_1012 dialog.
// Skip vs Java: the two SM_ITEM_USAGE_ANIMATION broadcasts bracketing the 2s item-use cast (start +
// end) aren't sent - no broadcast-to-nearby-players helper exists in this port (every caller of that
// packet in the codebase inlines its own connection-registry loop, which isn't reachable from a quest
// handler); the underlying 2s delay and state progression are kept so the quest stays completable.
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

namespace Quest.RiderQuests;

public sealed class _24052AFrozenCity : QuestHandlerBase
{
    private const int QuestIdConst = 24052;
    private const int FrozenNpc = 204753;
    private const int BossMob   = 233864;
    private const int Item1 = 182215378;
    private const int Item2 = 182215379;
    private const int Item3 = 182215380;
    private const string ItemUseZone = "DF3_ITEMUSEAREA_Q2056";

    private readonly IItemDao _itemDao;

    public _24052AFrozenCity(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestItem(Item1, QuestId);
        engine.RegisterQuestItem(Item2, QuestId);
        engine.RegisterQuestItem(Item3, QuestId);
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterQuestNpc(FrozenNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(BossMob).OnKill.Add(QuestId);
        engine.RegisterOnLevelUp(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, 24040, isZoneMission: true, ct);

    public override ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnKillEventAsync(env, conn, BossMob, 4, reward: true, ct);

    public override async ValueTask<bool> OnQuestTimerEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 4) return false;
        await ChangeQuestStepAsync(conn, entry, 0, 0, toReward: false, ct);
        return true;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        int var0        = entry.GetVar(0);

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId != FrozenNpc) return false;

            var rewardDialog = DialogActionLookup.FromId(env.DialogId);
            if (rewardDialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);

            foreach (int itemId in new[] { Item1, Item2, Item3 })
            {
                var held = player.Inventory.FindByItemId(itemId);
                if (held is not null && held.Count > 0)
                    await RemoveQuestItemAsync(player, conn, _itemDao, itemId, held.Count, ct);
            }
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        if (entry.Status != QuestStatus.START) return false;

        if (targetId != FrozenNpc) return false;

        var dialog = DialogActionLookup.FromId(env.DialogId);
        if (dialog == DialogAction.QUEST_SELECT && var0 == 0)
            return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
        if (dialog == DialogAction.SELECT_ACTION_1012)
        {
            await PlayQuestMovieAsync(conn, player, 242, ct);
            return false;
        }
        if (dialog == DialogAction.SETPRO1 && var0 == 0)
        {
            await GiveQuestItemAsync(player, conn, _itemDao, Item1, 1, ct);
            await GiveQuestItemAsync(player, conn, _itemDao, Item2, 1, ct);
            await GiveQuestItemAsync(player, conn, _itemDao, Item3, 1, ct);
            entry.SetVar(0, var0 + 1);
            await UpdateQuestStatusAsync(conn, entry, ct);
            return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
        }
        return false;
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (!player.CurrentZones.Contains(ItemUseZone)) return false;

        var entry = player.Quests.Get(QuestId);
        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(2));
            try
            {
                if (entry is null) return;
                int var0 = entry.GetVar(0);
                if (var0 == 1)
                {
                    await PlayQuestMovieAsync(conn, player, 243, CancellationToken.None);
                    await RemoveQuestItemAsync(player, conn, _itemDao, itemId, 1, CancellationToken.None);
                    await ChangeQuestStepAsync(conn, entry, 0, var0 + 1, toReward: false, CancellationToken.None);
                }
                else if (var0 == 2)
                {
                    await PlayQuestMovieAsync(conn, player, 244, CancellationToken.None);
                    await RemoveQuestItemAsync(player, conn, _itemDao, itemId, 1, CancellationToken.None);
                    await ChangeQuestStepAsync(conn, entry, 0, var0 + 1, toReward: false, CancellationToken.None);
                }
                else if (var0 == 3 && entry.Status != QuestStatus.COMPLETE && entry.Status != QuestStatus.NONE)
                {
                    await RemoveQuestItemAsync(player, conn, _itemDao, itemId, 1, CancellationToken.None);
                    await PlayQuestMovieAsync(conn, player, 245, CancellationToken.None);
                    SpawnQuestNpc(220040000, 1, BossMob, 2085, 120, 372, 60);
                    StartQuestTimer(new QuestEnv(null, player, QuestId, 0), conn, 180);
                    await ChangeQuestStepAsync(conn, entry, 0, var0 + 1, toReward: false, CancellationToken.None);
                }
            }
            catch { }
        });
        return true;
    }
}
