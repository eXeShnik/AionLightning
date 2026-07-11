// Port of Java data/scripts/system/handlers/quest/altgard/_2014ScoutitOut.java (MrPoke/vlog).
// Talk to Olenja (203606), loot the Suspicious Document (700136), pick up the report item
// (182203015), report to Hunmir (203633), destroy the Lepharist Escort Wagon (700135), turn in
// at Nokir (203631). Zone-mission chain, level-up gated.
using System.Linq;
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

namespace Quest.Altgard;

public sealed class _2014ScoutitOut : QuestHandlerBase
{
    private const int QuestIdConst   = 2014;
    private const int OlenjaNpc      = 203606;
    private const int DocumentObj    = 700136;
    private const int HunmirNpc      = 203633;
    private const int WagonNpc       = 700135;
    private const int NokirNpc       = 203631;
    private const int ReportItemId   = 182203015;

    public _2014ScoutitOut(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterItemGet(ReportItemId, QuestId);
        engine.RegisterQuestNpc(WagonNpc).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(OlenjaNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(DocumentObj).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(HunmirNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(NokirNpc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 2200, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;
        int var = entry.GetVar(0);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.START)
        {
            switch (targetId)
            {
                case OlenjaNpc:
                    switch (dialog)
                    {
                        case DialogAction.QUEST_SELECT when var == 0:
                            return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                        case DialogAction.QUEST_SELECT when var == 2:
                            var report = player.Inventory.FindByItemId(ReportItemId);
                            long reportCount = report?.Count ?? 0;
                            return await SendQuestDialogAsync(conn, targetObjId, reportCount == 0 ? 1438 : 1352, ct);
                        case DialogAction.SETPRO1:
                            return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                        case DialogAction.FINISH_DIALOG:
                            return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                        default:
                            return false;
                    }
                case DocumentObj:
                    return dialog == DialogAction.USE_OBJECT && var == 1; // loot
                case HunmirNpc:
                    if (dialog == DialogAction.QUEST_SELECT && var == 3)
                        return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                    if (dialog == DialogAction.SETPRO3)
                        return await DefaultCloseDialogAsync(env, conn, 3, 4, ct);
                    return false;
                default:
                    return false;
            }
        }
        if (entry.Status == QuestStatus.REWARD && targetId == NokirNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }

    public override async ValueTask<bool> OnItemGetAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != ReportItemId) return false;
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 1) return false;
        await ChangeQuestStepAsync(conn, entry, 0, 2, toReward: false, ct);
        return true;
    }

    public override ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnKillEventAsync(env, conn, WagonNpc, startVar: 4, reward: true, ct);
}
