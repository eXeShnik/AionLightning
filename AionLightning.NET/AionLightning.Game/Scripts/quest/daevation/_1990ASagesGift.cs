// Port of Java data/scripts/system/handlers/quest/daevation/_1990ASagesGift.java (kecimis, Tiger, reworked vlog).
// Start at Fermina (203771): hand in collect items (CHECK_USER_HAS_QUEST_ITEM, var0 0->1), SETPRO2
// (var0 1->2) begins the hunt - kill Strange Lake Spirit (256617 x30), Lava Hoverstone (253721/253720
// x30) and Disturbed Resident (254514/254513 x30); when all three counters are full, var1 is set to 60
// and the objective completes. SETPRO3 sets var0=3; SELECT_ACTION_2035 at var0 3 turns in when DP is
// exactly max and a Divine Incense Burner (186000040) is held - it is consumed, DP is zeroed and the
// quest flips to REWARD (dialog 5).
//
// Deviations vs Java (state transitions preserved):
//  - Java packs the three kill counters (A/B/C, each <30) into var0 via non-standard 7-bit shifting
//    (setQuestVar(C<<21 | B<<14 | A<<7 | 2)) while keeping A/B/C in SHARED, non-persisted handler
//    instance fields (a latent Java bug: the counts reset to 0 on relog / are shared across players).
//    The .NET QuestEntry uses 6-bit base-64 var slots (SetVar clamps 0..63) and cannot hold that packed
//    value, so the counters are stored in discrete, persisted slots var2/var3/var4 instead - same intent,
//    and it fixes the relog-loss/shared-state bug. var0 stays 2 and var1 carries the 60 completion flag,
//    exactly as the dialog reads them (getQuestVarById(0)/(1)).
//  - Java's start dialog branched on whether the Daevanion armor set (itemSetPartsEquipped) is worn
//    (4762 vs 4848) - no equipment set-parts API ported, so the eligible page (4762) is shown (matches
//    the _2990 sibling precedent).
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

namespace Quest.Daevation;

public sealed class _1990ASagesGift : QuestHandlerBase
{
    private const int QuestIdConst      = 1990;
    private const int Fermina           = 203771;
    private const int StrangeLakeSpirit = 256617;                 // counter A -> var2
    private const int LavaHoverstoneA   = 253721;                 // counter B -> var3
    private const int LavaHoverstoneB   = 253720;
    private const int DisturbedResidentA = 254514;               // counter C -> var4
    private const int DisturbedResidentB = 254513;
    private const int IncenseBurner     = 186000040;
    private const int HuntTarget        = 30;

    private readonly IItemDao _itemDao;

    public _1990ASagesGift(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        var fermina = engine.RegisterQuestNpc(Fermina);
        fermina.OnQuestStart.Add(QuestId);
        fermina.OnTalk.Add(QuestId);
        foreach (int mob in new[] { StrangeLakeSpirit, LavaHoverstoneA, LavaHoverstoneB, DisturbedResidentA, DisturbedResidentB })
            engine.RegisterQuestNpc(mob).OnKill.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);
        var entry       = player.Quests.Get(QuestId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId == Fermina)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    // note: Java shows 4848 when the Daevanion armor set is not worn; no set-parts API ported.
                    return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId != Fermina) return false;
            int var  = entry.GetVar(0);
            int var1 = entry.GetVar(1);
            long burner = player.Inventory.FindByItemId(IncenseBurner)?.Count ?? 0;

            if (dialog == DialogAction.QUEST_SELECT)
            {
                if (var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (var == 2 && var1 == 60) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (var == 3) return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                // Java switch fallthrough: QUEST_SELECT drops into CHECK_USER_HAS_QUEST_ITEM.
            }
            if (dialog == DialogAction.QUEST_SELECT || dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                return await CheckQuestItemsAsync(env, conn, _itemDao, 0, 1, reward: false, 10000, 10001, ct);
            if (dialog == DialogAction.SELECT_ACTION_2035)
            {
                if (player.Dp == player.MaxDp && burner >= 1)
                {
                    await RemoveQuestItemAsync(player, conn, _itemDao, IncenseBurner, 1, ct);
                    player.Dp = 0;
                    entry.Status = QuestStatus.REWARD; // Java changeQuestStep(env, 3, 3, true) - var0 stays 3, flips REWARD
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                }
                return await SendQuestDialogAsync(conn, targetObjId, 2120, ct);
            }
            if (dialog == DialogAction.SETPRO2)
                return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
            if (dialog == DialogAction.SETPRO3)
            {
                entry.SetVar(0, 3);
                await UpdateQuestStatusAsync(conn, entry, ct);
                return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
            }
            if (dialog == DialogAction.FINISH_DIALOG)
                return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
            return false;
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == Fermina)
                return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (entry.GetVar(0) != 2) return false;

        int a = entry.GetVar(2), b = entry.GetVar(3), c = entry.GetVar(4);
        int targetId = env.TargetId;

        if (targetId == StrangeLakeSpirit)
        {
            if (a >= 0 && a < HuntTarget) { entry.SetVar(2, a + 1); await UpdateQuestStatusAsync(conn, entry, ct); }
        }
        else if (targetId == LavaHoverstoneA || targetId == LavaHoverstoneB)
        {
            if (b >= 0 && b < HuntTarget) { entry.SetVar(3, b + 1); await UpdateQuestStatusAsync(conn, entry, ct); }
        }
        else if (targetId == DisturbedResidentA || targetId == DisturbedResidentB)
        {
            if (c >= 0 && c < HuntTarget) { entry.SetVar(4, c + 1); await UpdateQuestStatusAsync(conn, entry, ct); }
        }

        if (entry.GetVar(0) == 2 && entry.GetVar(2) == HuntTarget && entry.GetVar(3) == HuntTarget && entry.GetVar(4) == HuntTarget)
        {
            entry.SetVar(1, 60);
            await UpdateQuestStatusAsync(conn, entry, ct);
            return true;
        }
        return false;
    }
}
