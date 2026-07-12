// Port of Java data/scripts/system/handlers/quest/danaria/_23054AndBusinessIsGood.java.
// Accept at Kisping (801125) gives Kisping's Report (182213441). Two convergent paths advance var
// 0->1: talking through Ivolk's (801127) SETPRO1 dialog, or using either registered quest item
// directly (Java's onItemUseEvent doesn't discriminate by item id - it fires for both
// 182213441/182213442 - and additionally grants Mission Report (182213442) and flips straight to
// reward, a same-var-value transition since changeQuestStep(1,2,true) "ignores" nextStep when
// reward=true in Java's original, so var stays 1 rather than becoming 2 - ported as a raw var/status
// write to match exactly). Turn in at Melkorka (801128), which removes both report items.
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

namespace Quest.Danaria;

public sealed class _23054AndBusinessIsGood : QuestHandlerBase
{
    private const int QuestIdConst        = 23054;
    private const int KispingNpc          = 801125;
    private const int IvolkNpc            = 801127;
    private const int MelkorkaNpc         = 801128;
    private const int ReportItemId        = 182213441;
    private const int MissionReportItemId = 182213442;

    private readonly IItemDao _itemDao;

    public _23054AndBusinessIsGood(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(KispingNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(KispingNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(IvolkNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(MelkorkaNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestItem(ReportItemId, QuestId);
        engine.RegisterQuestItem(MissionReportItemId, QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId != KispingNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (env.DialogId == (int)DialogAction.QUEST_ACCEPT_1)
            {
                if (!await GiveQuestItemAsync(player, conn, _itemDao, ReportItemId, 1, ct)) return true;
            }
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == IvolkNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO1)
                {
                    entry.SetVar(0, entry.GetVar(0) + 1);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                }
                return false;
            }
            if (targetId == MelkorkaNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.SELECT_QUEST_REWARD)
                {
                    entry.SetVar(0, 2);
                    entry.Status = QuestStatus.REWARD;
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    await RemoveQuestItemAsync(player, conn, _itemDao, ReportItemId, 1, ct);
                    await RemoveQuestItemAsync(player, conn, _itemDao, MissionReportItemId, 1, ct);
                    return await SendQuestEndDialogAsync(env, conn, ct);
                }
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == MelkorkaNpc)
        {
            if (dialog == DialogAction.SELECT_QUEST_REWARD)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 0) return false;

        entry.SetVar(0, 1);
        entry.Status = QuestStatus.REWARD;
        await UpdateQuestStatusAsync(conn, entry, ct);
        await GiveQuestItemAsync(player, conn, _itemDao, MissionReportItemId, 1, ct);
        return true;
    }
}
