// Port of Java data/scripts/system/handlers/quest/crafting/_29038MasterCooksPotential.java (Thuatan, reworked vlog).
// Asmodian mirror of _19038: four cooking stages via Daraia (204101, RecipeNpc) — QUEST_SELECT for
// instructions, a SETPRO button to pay 6500 kinah and receive the stage material (var 0->2, 3->5,
// 6->8, 9->11); craft the proof item; a failed craft rolls the stage back one (2->1, 5->4, 8->7,
// 11->10) via the onFailCraft hook; then hand the finished proof to Lainita (204100, TurnInNpc) to
// advance (2->3, 5->6, 8->9, 11->REWARD). Kinah is modeled as inventory item 182400001 per the
// established "Kinah as item" convention (see _19057MasterConstructorsPotential and migration_plan.md).
// Java switch fallthroughs: the SETPRO cases carry no `break`, so out-of-order dialogs would cascade
// (and re-charge kinah). Per the _19057 precedent the clean per-var branches below preserve the real
// state machine and drop Java's exploit-path multi-charge; the intended inner recipe cascade
// (var 1/4/7/10) IS preserved via `goto case`.
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

namespace Quest.Crafting;

public sealed class _29038MasterCooksPotential : QuestHandlerBase
{
    private const int QuestIdConst = 29038;
    private const int TurnInNpc = 204100; // Lainita
    private const int RecipeNpc = 204101; // Daraia
    private const int KinahItemId = 182400001;
    private const long StageCost = 6500;

    private const int Recipe1Id = 155007241;
    private const int Recipe2Id = 155007242;
    private const int Recipe3Id = 155007243;
    private const int Recipe4Id = 155007244;

    private const int Material1Id = 152207202;
    private const int Material2Id = 152207203;
    private const int Material3Id = 152207204;
    private const int Material4Id = 152207205;

    private const int Proof1Id = 182207907;
    private const int Proof2Id = 182207908;
    private const int Proof3Id = 182207909;
    private const int Proof4Id = 182207910;

    private readonly IItemDao _itemDao;

    public _29038MasterCooksPotential(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        var lainita = engine.RegisterQuestNpc(TurnInNpc);
        lainita.OnQuestStart.Add(QuestId);
        lainita.OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(RecipeNpc).OnTalk.Add(QuestId);
        RegisterOnFailCraft(engine, Proof1Id);
        RegisterOnFailCraft(engine, Proof2Id);
        RegisterOnFailCraft(engine, Proof3Id);
        RegisterOnFailCraft(engine, Proof4Id);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry = player.Quests.Get(QuestId);
        var status = entry?.Status ?? QuestStatus.NONE;
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (status == QuestStatus.NONE)
        {
            if (targetId != TurnInNpc) return false;
            return dialog == DialogAction.QUEST_SELECT
                ? await SendQuestDialogAsync(conn, targetObjId, 4762, ct)
                : await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (status == QuestStatus.START)
        {
            int var = entry!.GetVar(0);

            if (targetId == RecipeNpc)
            {
                long kinah = player.Inventory.FindByItemId(KinahItemId)?.Count ?? 0;
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT:
                        if (kinah < StageCost)
                            return await SendQuestDialogAsync(conn, targetObjId, 4400, ct);
                        switch (var)
                        {
                            case 0: return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                            case 3: return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                            case 6: return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                            case 9: return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                            // Java switch fallthrough: a known recipe cascades to the next stage's check.
                            case 1:
                                if (!player.KnownRecipes.Contains(Recipe1Id)) return await SendQuestDialogAsync(conn, targetObjId, 4081, ct);
                                goto case 4;
                            case 4:
                                if (!player.KnownRecipes.Contains(Recipe2Id)) return await SendQuestDialogAsync(conn, targetObjId, 4166, ct);
                                goto case 7;
                            case 7:
                                if (!player.KnownRecipes.Contains(Recipe3Id)) return await SendQuestDialogAsync(conn, targetObjId, 4251, ct);
                                goto case 10;
                            case 10:
                                if (!player.KnownRecipes.Contains(Recipe4Id)) return await SendQuestDialogAsync(conn, targetObjId, 4336, ct);
                                break;
                        }
                        return false;
                    case DialogAction.SETPRO10:
                        await RemoveQuestItemAsync(player, conn, _itemDao, KinahItemId, StageCost, ct);
                        if (var == 0) return await DefaultCloseDialogAsync(env, conn, _itemDao, 0, 2, false, false, Material1Id, 1, 0, 0, ct);
                        if (var == 1) return await DefaultCloseDialogAsync(env, conn, _itemDao, 1, 2, false, false, Material1Id, 1, 0, 0, ct);
                        return false;
                    case DialogAction.SETPRO20:
                        await RemoveQuestItemAsync(player, conn, _itemDao, KinahItemId, StageCost, ct);
                        if (var == 3) return await DefaultCloseDialogAsync(env, conn, _itemDao, 3, 5, false, false, Material2Id, 1, 0, 0, ct);
                        if (var == 4) return await DefaultCloseDialogAsync(env, conn, _itemDao, 4, 5, false, false, Material2Id, 1, 0, 0, ct);
                        return false;
                    case DialogAction.SETPRO30:
                        await RemoveQuestItemAsync(player, conn, _itemDao, KinahItemId, StageCost, ct);
                        if (var == 6) return await DefaultCloseDialogAsync(env, conn, _itemDao, 6, 8, false, false, Material3Id, 1, 0, 0, ct);
                        if (var == 7) return await DefaultCloseDialogAsync(env, conn, _itemDao, 7, 8, false, false, Material3Id, 1, 0, 0, ct);
                        return false;
                    case DialogAction.SETPRO40:
                        await RemoveQuestItemAsync(player, conn, _itemDao, KinahItemId, StageCost, ct);
                        if (var == 9) return await DefaultCloseDialogAsync(env, conn, _itemDao, 9, 11, false, false, Material4Id, 1, 0, 0, ct);
                        if (var == 10) return await DefaultCloseDialogAsync(env, conn, _itemDao, 10, 11, false, false, Material4Id, 1, 0, 0, ct);
                        return false;
                    case DialogAction.FINISH_DIALOG:
                        return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                }
                return false;
            }

            if (targetId == TurnInNpc)
            {
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT:
                        switch (var)
                        {
                            case 2: return await SendQuestDialogAsync(conn, targetObjId, 1097, ct);
                            case 5: return await SendQuestDialogAsync(conn, targetObjId, 1438, ct);
                            case 8: return await SendQuestDialogAsync(conn, targetObjId, 1779, ct);
                            case 11: return await SendQuestDialogAsync(conn, targetObjId, 2120, ct);
                        }
                        return false;
                    case DialogAction.SETPRO11:
                        return await CheckItemExistenceAsync(env, conn, targetObjId, 2, 3, false, Proof1Id, 1, true, 1182, 2716, ct);
                    case DialogAction.SETPRO21:
                        return await CheckItemExistenceAsync(env, conn, targetObjId, 5, 6, false, Proof2Id, 1, true, 1523, 3057, ct);
                    case DialogAction.SETPRO31:
                        return await CheckItemExistenceAsync(env, conn, targetObjId, 8, 9, false, Proof3Id, 1, true, 1864, 3398, ct);
                    case DialogAction.SETPRO41:
                        return await CheckItemExistenceAsync(env, conn, targetObjId, 11, 11, true, Proof4Id, 1, true, 5, 3057, ct);
                    case DialogAction.FINISH_DIALOG:
                        return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                }
            }
            return false;
        }

        if (status == QuestStatus.REWARD)
        {
            if (targetId == TurnInNpc)
                return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }

    public override async ValueTask<bool> OnFailCraftAsync(QuestEnv env, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        switch (itemId)
        {
            case Proof1Id:
                if (entry.GetVar(0) == 2) await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: false, ct);
                return true;
            case Proof2Id:
                if (entry.GetVar(0) == 5) await ChangeQuestStepAsync(conn, entry, 0, 4, toReward: false, ct);
                return true;
            case Proof3Id:
                if (entry.GetVar(0) == 8) await ChangeQuestStepAsync(conn, entry, 0, 7, toReward: false, ct);
                return true;
            case Proof4Id:
                if (entry.GetVar(0) == 11) await ChangeQuestStepAsync(conn, entry, 0, 10, toReward: false, ct);
                return true;
        }
        return false;
    }

    // Java QuestHandler.checkItemExistence(env, step, nextStep, reward, itemId, count, remove, okId, failId, 0, 0):
    // when var0 == step and the player holds >= count of itemId, optionally remove it, advance the
    // step (or flip to REWARD), and show okId; otherwise show failId. No-op (false) when var0 != step.
    private async ValueTask<bool> CheckItemExistenceAsync(QuestEnv env, GsClientConnection conn, int targetObjId,
        int step, int nextStep, bool reward, int itemId, long itemCount, bool remove, int checkOkId, int checkFailId, CancellationToken ct)
    {
        var player = env.Player;
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.GetVar(0) != step) return false;

        if ((player.Inventory.FindByItemId(itemId)?.Count ?? 0) >= itemCount)
        {
            if (remove) await RemoveQuestItemAsync(player, conn, _itemDao, itemId, itemCount, ct);
            await ChangeQuestStepAsync(conn, entry, 0, nextStep, reward, ct);
            return await SendQuestDialogAsync(conn, targetObjId, checkOkId, ct);
        }
        return await SendQuestDialogAsync(conn, targetObjId, checkFailId, ct);
    }
}
