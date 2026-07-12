// Port of Java data/scripts/system/handlers/quest/greater_stigma/_3931HowToUseStigma.java
// (kecimis). Talk to Miriya (203711) to start; Koruchinerk (798321) hands in the quest_data.xml
// collect-items to receive Kohrunerk's Belt (182206080, var 1->2); Kohrunerk (279005, holding the
// belt) trades it for the Stigma Manual (182206081) and flips to REWARD; turn in at Miriya (only
// reachable while still carrying exactly one Stigma Manual, matching Java). The two
// registerQuestItem calls mirror Java's registration for onItemUseEvent, which the Java source
// never actually implements for this quest (dead registration, kept for fidelity — no override
// needed since the engine's default OnItemUseAsync is a no-op).
// Java's inner-switch fallthroughs (QUEST_SELECT -> CHECK_USER_HAS_QUEST_ITEM -> SETPRO1 at
// Koruchinerk once var has advanced past the guarded value; QUEST_SELECT -> SET_SUCCEED at
// Kohrunerk) are intentional shortcuts, ported via explicit ORs (same precedent as
// pandaemonium/_4966GrowthNinissFirstCharm).
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.GreaterStigma;

public sealed class _3931HowToUseStigma : QuestHandlerBase
{
    private const int QuestIdConst  = 3931;
    private const int MiriyaNpc     = 203711;
    private const int KoruchinerkNpc = 798321;
    private const int KohrunerkNpc  = 279005;
    private const int BeltItem      = 182206080;
    private const int ManualItem    = 182206081;

    private readonly IItemDao _itemDao;

    public _3931HowToUseStigma(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(MiriyaNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestItem(BeltItem, QuestId);
        engine.RegisterQuestItem(ManualItem, QuestId);
        engine.RegisterQuestNpc(KoruchinerkNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(KohrunerkNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(MiriyaNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null)
        {
            if (targetId != MiriyaNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        int var = entry.GetVar(0);

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId != MiriyaNpc || (player.Inventory.FindByItemId(ManualItem)?.Count ?? 0) != 1)
                return false;
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        if (entry.Status != QuestStatus.START) return false;

        if (targetId == KoruchinerkNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 0)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog == DialogAction.QUEST_SELECT && var == 1)
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            if (env.DialogId == (int)DialogAction.CHECK_USER_HAS_QUEST_ITEM && var == 1)
                return await CheckQuestItemsAsync(env, conn, _itemDao, 1, 2, reward: false, checkOkId: 10000, checkFailId: 10001, giveItemId: BeltItem, giveItemCount: 1, ct);
            if (dialog == DialogAction.SETPRO1
                || (dialog == DialogAction.QUEST_SELECT && var != 0 && var != 1)
                || (env.DialogId == (int)DialogAction.CHECK_USER_HAS_QUEST_ITEM && var != 1))
            {
                if (var == 0) entry.SetVar(0, 1);
                await UpdateQuestStatusAsync(conn, entry, ct);
                await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                return true;
            }
            return false;
        }

        if (targetId == KohrunerkNpc && (player.Inventory.FindByItemId(BeltItem)?.Count ?? 0) == 1)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 2)
                return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
            if (dialog == DialogAction.SET_SUCCEED || (dialog == DialogAction.QUEST_SELECT && var != 2))
            {
                if (var == 2)
                    await RemoveQuestItemAsync(player, conn, _itemDao, BeltItem, 1, ct);
                if (!await GiveQuestItemAsync(player, conn, _itemDao, ManualItem, 1, ct))
                    return true;
                await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                entry.Status = QuestStatus.REWARD;
                await UpdateQuestStatusAsync(conn, entry, ct);
                return true;
            }
            return false;
        }

        return false;
    }
}
