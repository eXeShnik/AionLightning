// Port of Java data/scripts/system/handlers/quest/daevation/_2990MakingTheDaevanionWeapon.java (kecimis).
// Start at Kanensa (204146): hand in collect items (CHECK_USER_HAS_QUEST_ITEM, var0 0->1), then via
// SETPRO2 (var0 1->2) begin the hunt - kill Strange Lake Spirit (256617), Lava Hoverstone (253720)
// and Disturbed Resident (254513), each raising its own counter (var1->60, var2->120, var3->240);
// when all are full the objective completes. SETPRO3 sets var0=3; SELECT_ACTION_2035 at var0 3 turns
// in when DP is exactly 4000 and a Divine Incense Burner (186000040) is held - it is consumed, DP is
// zeroed and the quest flips to REWARD (dialog 5).
// Skip vs Java: the start dialog branched on whether the Daevanion armor set (itemSetPartsEquipped)
// is worn (4848 vs 4762) - no equipment set-parts API ported, so the eligible page (4762) is shown.
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

public sealed class _2990MakingTheDaevanionWeapon : QuestHandlerBase
{
    private const int QuestIdConst = 2990;
    private const int Kanensa      = 204146;
    private const int StrangeLakeSpirit = 256617;
    private const int LavaHoverstone = 253720;
    private const int DisturbedResident = 254513;
    private const int IncenseBurner = 186000040;

    private readonly IItemDao _itemDao;

    public _2990MakingTheDaevanionWeapon(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(Kanensa).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(Kanensa).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(StrangeLakeSpirit).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(LavaHoverstone).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(DisturbedResident).OnKill.Add(QuestId);
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
            if (targetId == Kanensa)
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
            if (targetId != Kanensa) return false;
            int var  = entry.GetVar(0);
            int var1 = entry.GetVar(1);
            long burner = player.Inventory.FindByItemId(IncenseBurner)?.Count ?? 0;

            if (dialog == DialogAction.QUEST_SELECT)
            {
                if (var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (var == 2 && var1 == 60) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (var == 3 && burner > 0) return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                // Java switch fallthrough: QUEST_SELECT drops into CHECK_USER_HAS_QUEST_ITEM
            }
            if (dialog == DialogAction.QUEST_SELECT || dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
            {
                if (var == 0)
                    return await CheckQuestItemsAsync(env, conn, _itemDao, 0, 1, reward: false, 10000, 10001, ct);
                return false;
            }
            if (dialog == DialogAction.SELECT_ACTION_1352)
            {
                if (var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                // Java switch fallthrough: SELECT_ACTION_1352 drops into SELECT_ACTION_2035
            }
            if (dialog == DialogAction.SELECT_ACTION_1352 || dialog == DialogAction.SELECT_ACTION_2035)
            {
                if (var == 3)
                {
                    if (player.Dp == 4000 && burner > 0)
                    {
                        await RemoveQuestItemAsync(player, conn, _itemDao, IncenseBurner, 1, ct);
                        player.Dp = 0;
                        entry.Status = QuestStatus.REWARD;
                        await UpdateQuestStatusAsync(conn, entry, ct);
                        return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                    }
                    return await SendQuestDialogAsync(conn, targetObjId, 2120, ct);
                }
                return false;
            }
            if (dialog == DialogAction.SETPRO2)
            {
                if (var == 1)
                {
                    entry.SetVar(0, var + 1);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                }
                return false;
            }
            if (dialog == DialogAction.SETPRO3)
            {
                if (var == 2)
                {
                    entry.SetVar(0, 3);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                }
                return false;
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == Kanensa)
                return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int var  = entry.GetVar(0);
        int var1 = entry.GetVar(1);
        int var2 = entry.GetVar(2);
        int var3 = entry.GetVar(3);
        int targetId = env.TargetId;

        if (var == 2 && (targetId == StrangeLakeSpirit || targetId == LavaHoverstone || targetId == DisturbedResident))
        {
            switch (targetId)
            {
                case StrangeLakeSpirit:
                    if (var1 >= 0 && var1 < 60)
                    {
                        // Java: ++var1; setQuestVarById(1, var1 + 1) => stores original + 2
                        entry.SetVar(1, var1 + 2);
                        await UpdateQuestStatusAsync(conn, entry, ct);
                    }
                    break;
                case LavaHoverstone:
                    if (var2 >= 0 && var2 < 120)
                    {
                        // Java: ++var2; setQuestVarById(2, var2 + 3) => stores original + 4
                        entry.SetVar(2, var2 + 4);
                        await UpdateQuestStatusAsync(conn, entry, ct);
                    }
                    break;
                case DisturbedResident:
                    if (var3 >= 0 && var3 < 240)
                    {
                        // Java: ++var3; setQuestVarById(3, var3 + 7) => stores original + 8
                        entry.SetVar(3, var3 + 8);
                        await UpdateQuestStatusAsync(conn, entry, ct);
                    }
                    break;
            }
        }
        if (var == 2 && var1 == 60 && var2 == 120 && var3 == 240)
        {
            entry.SetVar(1, 60);
            await UpdateQuestStatusAsync(conn, entry, ct);
            return true;
        }
        return false;
    }
}
