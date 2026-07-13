// Port of Java data/scripts/system/handlers/quest/hidden_truth/_1097SwordofTranscendence.java
// (Hellboy, aion4Free; modified apozema). Talk to Pernos (790001) to advance var0->1, Anusis
// (798316) to advance var1->2, then Baoninerk (279034) to collect the check-item and flip to
// REWARD (grants Sword of Transcendence, 182206058); turn in at Pernos (removes the sword first).
// Skip vs Java: Pernos's SETPRO1 case additionally teleports the player to Verteron (110010000) via
// TeleportService2 — no TeleportService exists in this port (same precedent as
// quest/eltnen/_1430ATeleportationExperiment.cs and hidden_truth/_1096APastMission). The
// var/status transitions are kept.
// Divergence: Java returns `true` (handled, no further action) when the Sword-of-Transcendence give
// fails (bag full) inside the CHECK_USER_HAS_QUEST_ITEM branch; CheckQuestItemsAsync returns `false`
// in that case instead. Harmless here — no other quest is registered against Baoninerk +
// CHECK_USER_HAS_QUEST_ITEM — but noted for fidelity.
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

namespace Quest.HiddenTruth;

public sealed class _1097SwordofTranscendence : QuestHandlerBase
{
    private const int QuestIdConst  = 1097;
    private const int PernosNpc     = 790001;
    private const int AnusisNpc     = 798316;
    private const int BaoninerkNpc  = 279034;
    private const int SwordItemId   = 182206058;

    private readonly IItemDao _itemDao;

    public _1097SwordofTranscendence(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(PernosNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(AnusisNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(BaoninerkNpc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 1096, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int var         = entry.GetVar(0);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == PernosNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1 && var == 0)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
            }
            else if (targetId == AnusisNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 1)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO2 && var == 1)
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
            }
            else if (targetId == BaoninerkNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 2)
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SETPRO3 && var == 2)
                    return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM && var == 2)
                    return await CheckQuestItemsAsync(env, conn, _itemDao, 2, 2, reward: true,
                        checkOkId: 10000, checkFailId: 10001, giveItemId: SwordItemId, giveItemCount: 1, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == PernosNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            await RemoveQuestItemAsync(player, conn, _itemDao, SwordItemId, 1, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
