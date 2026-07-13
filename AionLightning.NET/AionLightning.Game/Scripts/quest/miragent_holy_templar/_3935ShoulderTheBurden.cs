// Port of Java data/scripts/system/handlers/quest/miragent_holy_templar/_3935ShoulderTheBurden.java
// (Nanou). Start at Lavirintos (203701); Ettamirel (203316, var 0->1); Jupion (203702, var 1->2);
// Elizar (203329, var 2->3, then a quest_data.xml collect-item check at var 3 advances 3->4 — Java's
// QuestService.collectItemCheck, ported via QuestHandlerBase.CheckQuestItemsAsync); report to Jucleas
// (203752) with the Oath Stone (186000080, removed) to flip to reward; turn in at Lavirintos.
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

public sealed class _3935ShoulderTheBurden : QuestHandlerBase
{
    private const int QuestIdConst  = 3935;
    private const int LavirintosNpc = 203701;
    private const int EttamirelNpc  = 203316;
    private const int JupionNpc     = 203702;
    private const int ElizarNpc     = 203329;
    private const int JucleasNpc    = 203752;
    private const int OathStoneItem = 186000080;

    private readonly IItemDao _itemDao;

    public _3935ShoulderTheBurden(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(LavirintosNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(LavirintosNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(EttamirelNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(JupionNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ElizarNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(JucleasNpc).OnTalk.Add(QuestId);
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
            if (targetId == EttamirelNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }
            if (targetId == JupionNpc)
            {
                if (var != 1) return false;
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO2)
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                return false;
            }
            if (targetId == ElizarNpc)
            {
                if (var == 2)
                {
                    if (dialog == DialogAction.QUEST_SELECT)
                        return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                    if (dialog == DialogAction.SETPRO3)
                        return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
                }
                if (var == 3)
                {
                    if (dialog == DialogAction.QUEST_SELECT)
                        return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                    if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                        return await CheckQuestItemsAsync(env, conn, _itemDao, 3, 4, reward: false, 10000, 10001, ct);
                }
                return false;
            }
            if (targetId == JucleasNpc)
            {
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT:
                        if (var == 4)
                            return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                        return false;
                    case DialogAction.SET_SUCCEED:
                        if (HasItem(player, OathStoneItem, 1))
                        {
                            await RemoveQuestItemAsync(player, conn, _itemDao, OathStoneItem, 1, ct);
                            return await DefaultCloseDialogAsync(env, conn, 4, 4, reward: true, sameNpc: false, ct);
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
