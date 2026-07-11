// Port of Java data/scripts/system/handlers/quest/eltnen/_1034DisappearingAether.java (Rhys2002).
// Zone-mission quest, part of the Kaidan Fortress chain started by _1300OrdersfromTelemachus:
// auto-(re)starts via OnLevelUp/OnZoneMissionEnd once quest 1300 is COMPLETE. Talk to Valerius
// (203903) to advance var 0->1; Lakaias (204032) drives a collect-check turn-in (var 1->2->3->4,
// reward at var 4); the Old Machine (700149) advances var 2->3 via useQuestObject.
// Note: Java also registers NPC 204501 for OnTalk but its onDialogEvent has no case for that
// target id (dead registration in the original) - kept here for 1:1 NPC-index parity, harmless.
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

namespace Quest.Eltnen;

public sealed class _1034DisappearingAether : QuestHandlerBase
{
    private const int QuestIdConst = 1034;
    private const int ValeriusNpc  = 203903;
    private const int LakaiasNpc   = 204032;
    private const int DeadEndNpc   = 204501;
    private const int OldMachineObj = 700149;

    private readonly IItemDao _itemDao;

    public _1034DisappearingAether(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(ValeriusNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(LakaiasNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(DeadEndNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(OldMachineObj).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 1300, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null) return false;

        int var = entry.GetVar(0);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId != ValeriusNpc) return false;
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        if (entry.Status != QuestStatus.START) return false;

        if (targetId == ValeriusNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 0)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog == DialogAction.SETPRO1)
                return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
            return false;
        }

        if (targetId == LakaiasNpc)
        {
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT when var == 1:
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                case DialogAction.QUEST_SELECT when var == 3:
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                case DialogAction.QUEST_SELECT when var == 4:
                    return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                case DialogAction.CHECK_USER_HAS_QUEST_ITEM:
                    return await CheckQuestItemsAsync(env, conn, _itemDao, 4, 4, reward: true, checkOkId: 2035, checkFailId: 2120, ct);
                case DialogAction.SELECT_ACTION_1353:
                    await PlayQuestMovieAsync(conn, env.Player, 179, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 1353, ct);
                case DialogAction.SETPRO2:
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                case DialogAction.SETPRO3:
                    return await DefaultCloseDialogAsync(env, conn, 3, 4, ct);
                case DialogAction.FINISH_DIALOG:
                    return await DefaultCloseDialogAsync(env, conn, 4, 4, ct);
                default:
                    return false;
            }
        }

        if (targetId == OldMachineObj && dialog == DialogAction.USE_OBJECT && var == 2)
            return await UseQuestObjectAsync(env, conn, step: 2, nextStep: 3, reward: false, dieObject: false, ct);

        return false;
    }
}
