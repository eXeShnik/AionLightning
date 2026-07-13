// Port of Java data/scripts/system/handlers/quest/miragent_holy_templar/_3933ClassPreceptorConsent.java
// (Nanou). Start at Lavirintos (203701); a 6-npc "recommendation letter" relay chain
// (203704->203705->203706->203707->801214->801215, var 0->6); report to Jucleas (203752) with the
// Oath Stone (186000080, removed) to flip to reward; turn in at Lavirintos.
// Java bug: the outer switch(targetId) is missing a `break;` after every case from 203704 through
// 801215, so falling through an unmatched dialog at 203704 cascades all the way into 203752's
// switch(dialog) unconditionally. The near-identical sibling quest _3934TheQuestForTemplars (same
// zone, same author, same structure) has explicit breaks after every case — confirming this is a
// copy/paste omission, not intended behavior. Fixed here to match the corrected sibling: each NPC is
// handled independently.
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

namespace Quest.MiragentHolyTemplar;

public sealed class _3933ClassPreceptorConsent : QuestHandlerBase
{
    private const int QuestIdConst  = 3933;
    private const int LavirintosNpc = 203701;
    private const int BoreasNpc     = 203704;
    private const int JumentisNpc   = 203705;
    private const int CharnaNpc     = 203706;
    private const int ThrasymedesNpc = 203707;
    private const int Npc801214     = 801214;
    private const int Npc801215     = 801215;
    private const int JucleasNpc    = 203752;
    private const int OathStoneItem = 186000080;

    private readonly IItemDao _itemDao;

    public _3933ClassPreceptorConsent(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(LavirintosNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(LavirintosNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(BoreasNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(JumentisNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(CharnaNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ThrasymedesNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(JucleasNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Npc801214).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Npc801215).OnTalk.Add(QuestId);
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
            int var = entry.GetVar(0);
            if (targetId == BoreasNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }
            if (targetId == JumentisNpc)
            {
                if (var != 1) return false;
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO2)
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                return false;
            }
            if (targetId == CharnaNpc)
            {
                if (var != 2) return false;
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SETPRO3)
                    return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
                return false;
            }
            if (targetId == ThrasymedesNpc)
            {
                if (var != 3) return false;
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                if (dialog == DialogAction.SETPRO4)
                    return await DefaultCloseDialogAsync(env, conn, 3, 4, ct);
                return false;
            }
            if (targetId == Npc801214)
            {
                if (var != 4) return false;
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.SETPRO5)
                    return await DefaultCloseDialogAsync(env, conn, 4, 5, ct);
                return false;
            }
            if (targetId == Npc801215)
            {
                if (var != 5) return false;
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                if (dialog == DialogAction.SETPRO6)
                    return await DefaultCloseDialogAsync(env, conn, 5, 6, ct);
                return false;
            }
            if (targetId == JucleasNpc)
            {
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT:
                        if (var == 6)
                            return await SendQuestDialogAsync(conn, targetObjId, 3057, ct);
                        return false;
                    case DialogAction.SET_SUCCEED:
                        if (HasItem(player, OathStoneItem, 1))
                        {
                            await RemoveQuestItemAsync(player, conn, _itemDao, OathStoneItem, 1, ct);
                            return await DefaultCloseDialogAsync(env, conn, 6, 6, reward: true, sameNpc: false, ct);
                        }
                        return await SendQuestDialogAsync(conn, targetObjId, 2461, ct);
                    case DialogAction.FINISH_DIALOG:
                        return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                    default:
                        return false;
                }
            }
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.REWARD && targetId == LavirintosNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }

    private static bool HasItem(Player player, int itemId, long count)
    {
        var item = player.Inventory.FindByItemId(itemId);
        return item is not null && item.Count >= count;
    }
}
