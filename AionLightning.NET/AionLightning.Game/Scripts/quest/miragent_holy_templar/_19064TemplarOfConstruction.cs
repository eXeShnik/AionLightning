// Port of Java data/scripts/system/handlers/quest/miragent_holy_templar/_19064TemplarOfConstruction.java
// (Cheatkiller). Start at Lavirintos (203701); a quest device/npc (798450, var 0->1); report to
// Jucleas (203752) who requires both 182213237 and 186000081 (1 each) to flip to reward, removing
// both; turn in at Lavirintos.
// Java's 203752 switch has QUEST_SELECT fall through (no break) into
// CHECK_USER_HAS_QUEST_ITEM_SIMPLE when the var/item guard fails, so a QUEST_SELECT click with both
// items already in hand also completes the step — reproduced with an explicit `goto case`.
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

public sealed class _19064TemplarOfConstruction : QuestHandlerBase
{
    private const int QuestIdConst  = 19064;
    private const int LavirintosNpc = 203701;
    private const int ConstructorNpc = 798450;
    private const int JucleasNpc    = 203752;
    private const int PartItem1     = 182213237;
    private const int PartItem2     = 186000081;

    private readonly IItemDao _itemDao;

    public _19064TemplarOfConstruction(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(LavirintosNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(LavirintosNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ConstructorNpc).OnTalk.Add(QuestId);
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
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);
            if (targetId == ConstructorNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }
            if (targetId == JucleasNpc)
            {
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT:
                        if (var == 1 && HasItem(player, PartItem1, 1) && HasItem(player, PartItem2, 1))
                            return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                        goto case DialogAction.CHECK_USER_HAS_QUEST_ITEM_SIMPLE;
                    case DialogAction.CHECK_USER_HAS_QUEST_ITEM_SIMPLE:
                        if (HasItem(player, PartItem1, 1) && HasItem(player, PartItem2, 1))
                        {
                            await RemoveQuestItemAsync(player, conn, _itemDao, PartItem1, 1, ct);
                            await RemoveQuestItemAsync(player, conn, _itemDao, PartItem2, 1, ct);
                            return await DefaultCloseDialogAsync(env, conn, 1, 1, reward: true, sameNpc: false, ct);
                        }
                        return false;
                    default:
                        return false;
                }
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == LavirintosNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
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
