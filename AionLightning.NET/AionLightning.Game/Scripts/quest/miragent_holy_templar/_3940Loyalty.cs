// Port of Java data/scripts/system/handlers/quest/miragent_holy_templar/_3940Loyalty.java (vlog,
// bobobear). Start at Lavirintos (203701); a quest_data.xml collect-item check (amulets) advances
// var 0->6; killing any of six Great Protectors (251002/251021/251018/251039/251033/251036)
// increments the RAW packed quest-var counter one at a time from 6 up to 306 (Java
// qs.setQuestVar(var+1) overwrites the whole packed int, not just var-slot 0 — reproduced here via
// entry.Step, the C# equivalent of QuestVars.getQuestVars()/setVar, rather than entry.SetVar(0, ...)
// which only touches the low slot and would corrupt the count once it crosses 64); back at
// Lavirintos, dialog SETPRO3 resets the packed var to 3 (qs.setQuestVar(3)); killing either Dredgion
// captain (214823/216850) while var==3 advances 3->4 (var-slot 0 write here, matching Java's
// defaultOnKillEvent/changeQuestStep, which always operate on slot 0 regardless of how "var" was
// read for display); report to Jucleas (203752): requires 4000+ DP, then the Divine Oath Stone
// (186000083) to flip to reward (DP is reset to 0 on success); turn in at Lavirintos. Java's
// QUEST_SELECT case at Lavirintos falls through (no break) into CHECK_USER_HAS_QUEST_ITEM when none
// of the var==0/306/4 branches match — reproduced with an explicit `goto case`.
// Java bug preserved: the REWARD-status branch at Lavirintos checks QUEST_SELECT (not USE_OBJECT,
// unlike most sibling quests in this zone) before falling back to sendQuestEndDialog — kept as
// authored.
using System.Linq;
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

namespace Quest.MiragentHolyTemplar;

public sealed class _3940Loyalty : QuestHandlerBase
{
    private const int QuestIdConst  = 3940;
    private const int LavirintosNpc = 203701;
    private const int JucleasNpc    = 203752;
    private const int DivineOathStoneItem = 186000083;

    private static readonly int[] DefenderNpcs = [251002, 251021, 251018, 251039, 251033, 251036];
    private static readonly int[] CaptainNpcs  = [214823, 216850];

    private readonly IItemDao _itemDao;

    public _3940Loyalty(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(LavirintosNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(LavirintosNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(JucleasNpc).OnTalk.Add(QuestId);
        foreach (int mob in DefenderNpcs)
            engine.RegisterQuestNpc(mob).OnKill.Add(QuestId);
        foreach (int mob in CaptainNpcs)
            engine.RegisterQuestNpc(mob).OnKill.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null)
        {
            if (targetId != LavirintosNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.Step; // Java: qs.getQuestVars().getQuestVars() (packed read)

            if (targetId == LavirintosNpc)
            {
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT:
                        if (var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                        if (var == 306) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                        if (var == 4) return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                        goto case DialogAction.CHECK_USER_HAS_QUEST_ITEM;
                    case DialogAction.CHECK_USER_HAS_QUEST_ITEM:
                        return await CheckQuestItemsAsync(env, conn, _itemDao, 0, 6, reward: false, 10000, 10001, ct);
                    case DialogAction.FINISH_DIALOG:
                        return await DefaultCloseDialogAsync(env, conn, 0, 0, ct);
                    case DialogAction.SETPRO3:
                        entry.Step = 3; // Java: qs.setQuestVar(3) (packed overwrite)
                        await UpdateQuestStatusAsync(conn, entry, ct);
                        return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                    case DialogAction.SETPRO5:
                        return await DefaultCloseDialogAsync(env, conn, 4, 5, ct);
                    default:
                        return false;
                }
            }

            if (targetId == JucleasNpc)
            {
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT:
                        if (var == 5)
                            return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                        return false;
                    case DialogAction.SELECT_ACTION_2718:
                        if (player.Dp >= 4000)
                            return await CheckItemExistenceAsync(env, conn, 5, 5, reward: false, DivineOathStoneItem, 1, remove: true, 2718, 2887, 0, 0, ct);
                        return await SendQuestDialogAsync(conn, targetObjId, 2802, ct);
                    case DialogAction.SET_SUCCEED:
                        player.Dp = 0;
                        return await DefaultCloseDialogAsync(env, conn, 5, 5, reward: true, sameNpc: false, ct);
                    case DialogAction.FINISH_DIALOG:
                        return await DefaultCloseDialogAsync(env, conn, 5, 5, ct);
                    default:
                        return false;
                }
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == LavirintosNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int var = entry.Step; // Java: qs.getQuestVars().getQuestVars() (packed read)
        if (var >= 6 && var < 306)
        {
            if (!DefenderNpcs.Contains(env.TargetId)) return false;
            entry.Step = var + 1; // Java: qs.setQuestVar(var + 1) (packed overwrite)
            await UpdateQuestStatusAsync(conn, entry, ct);
            return true;
        }
        if (var == 3)
            return await DefaultOnKillEventAsync(env, conn, CaptainNpcs, 3, 4, ct);

        return false;
    }

    /// <summary>Java QuestHandler.checkItemExistence(step, nextStep, reward, itemId, itemCount, remove,
    /// checkOkId, checkFailId, giveItemId, giveItemCount) — an explicit item id/count gate, distinct
    /// from the quest_data.xml collect-items list <see cref="QuestHandlerBase.CheckQuestItemsAsync"/> reads.
    /// Gates on var-slot 0 (Java's getQuestVarById(0)), same as the rest of this file's step helpers.</summary>
    private async ValueTask<bool> CheckItemExistenceAsync(QuestEnv env, GsClientConnection conn,
        int step, int nextStep, bool reward, int itemId, long itemCount, bool remove,
        int checkOkId, int checkFailId, int giveItemId, long giveItemCount, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetObjId = env.Target?.ObjectId ?? 0;
        if (entry is null || entry.GetVar(0) != step) return false;

        var existing = player.Inventory.FindByItemId(itemId);
        bool has = existing is not null && existing.Count >= itemCount;
        if (has && remove)
            has = await RemoveQuestItemAsync(player, conn, _itemDao, itemId, itemCount, ct);

        if (!has)
            return await SendQuestDialogAsync(conn, targetObjId, checkFailId, ct);

        if (giveItemId != 0 && giveItemCount != 0 && !await GiveQuestItemAsync(player, conn, _itemDao, giveItemId, giveItemCount, ct))
            return false;

        await ChangeQuestStepAsync(conn, entry, 0, nextStep, reward, ct);
        return await SendQuestDialogAsync(conn, targetObjId, checkOkId, ct);
    }
}
