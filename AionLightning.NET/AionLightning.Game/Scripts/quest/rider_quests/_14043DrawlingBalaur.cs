// Port of Java data/scripts/system/handlers/quest/rider_quests/_14043DrawlingBalaur.java (pralinka).
// Zone-mission sub-quest of 14040: talk to 278532 (var0 0->1), a multi-step chain at 798026
// (var0 1->2 via SETPRO2 free or SETPRO11 for 20000 kinah, var0 2->3), 798025 (var0 2->3), 279019
// (var0 3->4, hands out item 182215352), back to 798026 (var0 4->5, swaps item 182215352 for
// 182215351, var0 6->REWARD via SETPRO7); using item 182215351 advances var0 by one; turn in at 278532.
// Java bug: onDialogEvent's switch on 798026 had no breaks, so a QUEST_SELECT (with var0 outside
// {1,4,6}) or a direct SETPRO2 call with var0 != 1 fell through into SETPRO11's else branch and
// showed the "insufficient kinah" dialog (1355) even though no kinah check was relevant. Fixed
// here so 1355 only shows when the dialog actually is SETPRO11 and its own guard fails.
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

public sealed class _14043DrawlingBalaur : QuestHandlerBase
{
    private const int QuestIdConst = 14043;
    private const int Npc278532 = 278532;
    private const int Npc798026 = 798026;
    private const int Npc798025 = 798025;
    private const int Npc279019 = 279019;
    private const int OldItem   = 182215352;
    private const int SealedItem = 182215351;
    private const int KinahItemId = 182400001;

    private readonly IItemDao _itemDao;

    public _14043DrawlingBalaur(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestItem(SealedItem, QuestId);
        foreach (int npc in new[] { Npc278532, Npc798026, Npc798025, Npc279019 })
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, 14040, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        var dialog = DialogActionLookup.FromId(env.DialogId);
        int targetObjId = env.Target?.ObjectId ?? 0;

        if (entry.Status == QuestStatus.REWARD)
        {
            if (env.TargetId != Npc278532) return false;
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        if (entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);

        if (env.TargetId == Npc278532)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return var == 0 && await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog == DialogAction.SETPRO1)
                return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
            return false;
        }

        if (env.TargetId == Npc798026)
        {
            if (dialog == DialogAction.QUEST_SELECT)
            {
                if (var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (var == 4) return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (var == 6) return await SendQuestDialogAsync(conn, targetObjId, 3057, ct);
                return false;
            }
            if (dialog == DialogAction.SETPRO5)
            {
                if (var != 4) return false;
                await RemoveQuestItemAsync(player, conn, _itemDao, OldItem, 1, ct);
                if (!await GiveQuestItemAsync(player, conn, _itemDao, SealedItem, 1, ct)) return true;
                return await DefaultCloseDialogAsync(env, conn, 4, 5, ct);
            }
            if (dialog == DialogAction.SETPRO7)
                return await DefaultCloseDialogAsync(env, conn, 6, 6, reward: true, sameNpc: false, ct);
            if (dialog == DialogAction.SETPRO2)
                return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
            if (dialog == DialogAction.SETPRO11)
            {
                if (var == 1 && await TryDecreaseKinahAsync(player, conn, 20000, ct))
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                return await SendQuestDialogAsync(conn, targetObjId, 1355, ct);
            }
            if (dialog == DialogAction.SETPRO12)
                return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
            return false;
        }

        if (env.TargetId == Npc798025)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return var == 2 && await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
            if (dialog == DialogAction.SETPRO3)
                return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
            return false;
        }

        if (env.TargetId == Npc279019)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return var == 3 && await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
            if (dialog == DialogAction.SETPRO4)
            {
                if (var != 3) return false;
                if (!await GiveQuestItemAsync(player, conn, _itemDao, OldItem, 1, ct)) return true;
                return await DefaultCloseDialogAsync(env, conn, 3, 4, ct);
            }
            return false;
        }

        return false;
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != SealedItem) return false;
        var entry = player.Quests.Get(QuestId);
        if (entry is null) return false;

        await RemoveQuestItemAsync(player, conn, _itemDao, SealedItem, 1, ct);
        await ChangeQuestStepAsync(conn, entry, 0, entry.GetVar(0) + 1, toReward: false, ct);
        return true;
    }

    private async ValueTask<bool> TryDecreaseKinahAsync(Player player, GsClientConnection conn, long amount, CancellationToken ct)
    {
        var kinah = player.Inventory.FindByItemId(KinahItemId);
        if ((kinah?.Count ?? 0) < amount) return false;
        return await RemoveQuestItemAsync(player, conn, _itemDao, KinahItemId, amount, ct);
    }
}
