// Port of Java data/scripts/system/handlers/quest/reshanta/_1074FragmentofMemory.java (MetaWind/apozema).
// Talk to Michalis (278501, var 0->1), Pernos (790001, var 1->2), Lugbug (279029, var 2->3), then
// use the Artefact of the Inception (700355) to finish. Zone-mission chain, level-up gated on 1701.
// Skip vs Java: TeleportService2.teleportTo calls at Michalis (to Sanctum, 210010000) and Pernos
// (to Reshanta, 400010000) - no TeleportService2 in this port (same precedent as
// quest/eltnen/_1430ATeleportationExperiment.cs). The var/status transitions are kept so the quest
// stays completable without the cross-zone hop.
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

namespace Quest.Reshanta;

public sealed class _1074FragmentofMemory : QuestHandlerBase
{
    private const int QuestIdConst = 1074;
    private const int MichalisNpc  = 278501;
    private const int PernosNpc    = 790001;
    private const int LugbugNpc    = 279029;
    private const int ArtefactObj  = 700355;

    public _1074FragmentofMemory(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(MichalisNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(PernosNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(LugbugNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ArtefactObj).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 1701, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);
        int var         = entry.GetVar(0);

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId != LugbugNpc) return false;
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        if (entry.Status != QuestStatus.START) return false;

        switch (targetId)
        {
            case MichalisNpc:
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT when var == 0:
                        return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                    case DialogAction.SETPRO1 when var == 0:
                        return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                    default:
                        return false;
                }
            case PernosNpc:
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT when var == 1:
                        return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    case DialogAction.SETPRO2 when var == 1:
                        return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                    default:
                        return false;
                }
            case LugbugNpc:
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT when var == 2:
                        return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                    case DialogAction.SETPRO3 when var == 2:
                        entry.SetVar(0, 3);
                        await UpdateQuestStatusAsync(conn, entry, ct);
                        return await CloseDialogWindowAsync(conn, targetObjId, ct);
                    default:
                        return false;
                }
            case ArtefactObj:
                await UseQuestObjectAsync(env, conn, step: 3, nextStep: 3, reward: true, dieObject: false, ct);
                await PlayQuestMovieAsync(conn, player, 271, ct);
                return true;
            default:
                return false;
        }
    }
}
