// Port of Java data/scripts/system/handlers/quest/morheim/_2430SecretInformation.java (MrPoke remod By Nephis, reworked vlog).
// Offered at Sveinn (204327, OnQuestStart): three paid "skip ahead" entry points let the player pay
// kinah to start directly at var 1 (500k), var 3 (5000k) or var 7 (50000k) instead of the free var-0
// start. From there: Sveinn var 1->2 (gives item 182204221), Grall (204377, var 2) grants reward
// tier 0; Sveinn var 3->4, Hugorunerk (205244, var 4->5), Nicoyerk (798081, var 5->6), Bicorunerk
// (798082, var 6) grants reward tier 1; Sveinn var 7->8, Bolverk (204300, var 8, requires holding
// item 182204222) grants reward tier 2. Each reward tier is turned in independently at its own npc.
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

namespace Quest.Morheim;

public sealed class _2430SecretInformation : QuestHandlerBase
{
    private const int QuestIdConst = 2430;
    private const int SveinnNpc     = 204327;
    private const int GrallNpc      = 204377;
    private const int HugorunerkNpc = 205244;
    private const int NicoyerkNpc   = 798081;
    private const int BicorunerkNpc = 798082;
    private const int BolverkNpc    = 204300;
    private const int LetterItem    = 182204221;
    private const int SealItem      = 182204222;
    private const int KinahItemId   = 182400001;

    private static readonly int[] _npcIds =
        [SveinnNpc, GrallNpc, HugorunerkNpc, NicoyerkNpc, BicorunerkNpc, BolverkNpc];

    private readonly IItemDao _itemDao;

    public _2430SecretInformation(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(SveinnNpc).OnQuestStart.Add(QuestId);
        foreach (int npc in _npcIds)
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId != SveinnNpc) return false;
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT:
                    return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
                case DialogAction.ASK_QUEST_ACCEPT:
                    return await SendQuestDialogAsync(conn, targetObjId, 4, ct);
                case DialogAction.QUEST_REFUSE_1:
                    return await SendQuestDialogAsync(conn, targetObjId, 1004, ct);
                case DialogAction.QUEST_ACCEPT_1:
                    return await SendQuestDialogAsync(conn, targetObjId, 1003, ct);
                case DialogAction.FINISH_DIALOG:
                    return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                case DialogAction.SETPRO1:
                    return await StartAtStepAsync(conn, player, targetObjId, 500, 1, 1352, ct);
                case DialogAction.SETPRO3:
                    return await StartAtStepAsync(conn, player, targetObjId, 5000, 3, 2034, ct);
                case DialogAction.SETPRO7:
                    return await StartAtStepAsync(conn, player, targetObjId, 50000, 7, 3398, ct);
                default:
                    return false;
            }
        }

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);

            if (targetId == SveinnNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    if (var == 3) return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                    if (var == 7) return await SendQuestDialogAsync(conn, targetObjId, 3398, ct);
                    return false;
                }
                if (dialog == DialogAction.SETPRO2)
                    return await DefaultCloseDialogAsync(env, conn, _itemDao, 1, 2, reward: false, sameNpc: false, LetterItem, 1, 0, 0, ct);
                if (dialog == DialogAction.SETPRO4)
                    return await DefaultCloseDialogAsync(env, conn, 3, 4, ct);
                if (dialog == DialogAction.SETPRO8)
                    return await DefaultCloseDialogAsync(env, conn, 7, 8, ct);
                return false;
            }

            if (targetId == GrallNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return var == 2 && await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
                {
                    await RemoveQuestItemAsync(player, conn, _itemDao, LetterItem, 1, ct);
                    await ChangeQuestStepAsync(conn, entry, 0, 2, toReward: true, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                }
                return false;
            }

            if (targetId == HugorunerkNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return var == 4 && await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.SETPRO5)
                    return await DefaultCloseDialogAsync(env, conn, 4, 5, ct);
                return false;
            }

            if (targetId == NicoyerkNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return var == 5 && await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                if (dialog == DialogAction.SETPRO6)
                    return await DefaultCloseDialogAsync(env, conn, 5, 6, ct);
                return false;
            }

            if (targetId == BicorunerkNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return var == 6 && await SendQuestDialogAsync(conn, targetObjId, 3057, ct);
                if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
                {
                    await ChangeQuestStepAsync(conn, entry, 0, 6, toReward: true, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 6, ct);
                }
                return false;
            }

            if (targetId == BolverkNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var != 8) return false;
                    return await SendQuestDialogAsync(conn, targetObjId,
                        (player.Inventory.FindByItemId(SealItem)?.Count ?? 0) > 0 ? 3739 : 3825, ct);
                }
                if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
                {
                    await RemoveQuestItemAsync(player, conn, _itemDao, SealItem, 1, ct);
                    await ChangeQuestStepAsync(conn, entry, 0, 8, toReward: true, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 7, ct);
                }
                if (dialog == DialogAction.FINISH_DIALOG)
                    return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                return false;
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            int var = entry.GetVar(0);
            if (targetId == GrallNpc && var == 2)
                return await SendQuestEndDialogWithRewardAsync(env, conn, 0, ct);
            if (targetId == BicorunerkNpc && var == 6)
                return await SendQuestEndDialogWithRewardAsync(env, conn, 1, ct);
            if (targetId == BolverkNpc && var == 8)
                return await SendQuestEndDialogWithRewardAsync(env, conn, 2, ct);
        }

        return false;
    }

    /// <summary>Java QuestService.startQuest(env) + decreaseKinah + changeQuestStep: creates the
    /// quest entry (if the player can afford it) already parked at <paramref name="step"/>.</summary>
    private async ValueTask<bool> StartAtStepAsync(GsClientConnection conn, Player player, int targetObjId, long kinahCost, int step, int dialogId, CancellationToken ct)
    {
        if (!await TryDecreaseKinahAsync(player, conn, kinahCost, ct))
            return await SendQuestDialogAsync(conn, targetObjId, 1267, ct);

        await StartMissionAsync(conn, player, QuestStatus.START, ct);
        var entry = player.Quests.Get(QuestId)!;
        await ChangeQuestStepAsync(conn, entry, 0, step, toReward: false, ct);
        return await SendQuestDialogAsync(conn, targetObjId, dialogId, ct);
    }

    /// <summary>Java Inventory.tryDecreaseKinah(amount): deducts kinah if the player has enough, persisting the change.</summary>
    private async ValueTask<bool> TryDecreaseKinahAsync(Player player, GsClientConnection conn, long amount, CancellationToken ct)
    {
        var kinah = player.Inventory.FindByItemId(KinahItemId);
        if ((kinah?.Count ?? 0) < amount) return false;
        return await RemoveQuestItemAsync(player, conn, _itemDao, KinahItemId, amount, ct);
    }

    /// <summary>Java QuestHandler.sendQuestEndDialog(env, reward): SELECT_QUEST_REWARD/USE_OBJECT
    /// shows the tier-confirm page 5+reward; SELECTED_QUEST_REWARDn/NOREWARD actually completes
    /// with that reward tier.</summary>
    private async ValueTask<bool> SendQuestEndDialogWithRewardAsync(QuestEnv env, GsClientConnection conn, int rewardIndex, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.REWARD) return false;
        int targetObjId = env.Target?.ObjectId ?? 0;
        int dialogId = env.DialogId;

        if (dialogId >= (int)DialogAction.SELECTED_QUEST_REWARD1 && dialogId <= (int)DialogAction.SELECTED_QUEST_NOREWARD)
        {
            if (!await FinishQuestAsync(conn, env.Player, rewardIndex, ct)) return false;
            await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
            return true;
        }
        if (dialogId == (int)DialogAction.SELECT_QUEST_REWARD || dialogId == (int)DialogAction.USE_OBJECT)
            return await SendQuestDialogAsync(conn, targetObjId, 5 + rewardIndex, ct);
        return false;
    }
}
