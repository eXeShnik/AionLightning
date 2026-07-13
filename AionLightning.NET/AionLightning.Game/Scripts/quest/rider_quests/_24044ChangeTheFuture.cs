// Port of Java data/scripts/system/handlers/quest/rider_quests/_24044ChangeTheFuture.java (pralinka).
// Zone-mission sub-quest of 24040: talk to Scoda (278036, var0 0->1), Munin (203550, var0 1->2),
// Kasir (204207, var0 2->3), Lyeanenerk (798067, var0 3->4), Lugbug (279029, gives an artifact item,
// var0 4->5; SET_SUCCEED var0 6->reward), use the artifact at 700355 (var0 5->6, movie 291), turn in
// at Scoda.
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

namespace Quest.RiderQuests;

public sealed class _24044ChangeTheFuture : QuestHandlerBase
{
    private const int QuestIdConst = 24044;
    private const int ScodaNpc       = 278036;
    private const int MuninNpc       = 203550;
    private const int KasirNpc       = 204207;
    private const int LyeanenerkNpc  = 798067;
    private const int LugbugNpc      = 279029;
    private const int ArtifactNpc    = 700355;
    private const int ArtifactItem   = 188020000;

    private readonly IItemDao _itemDao;

    public _24044ChangeTheFuture(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(ScodaNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(MuninNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(KasirNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(LyeanenerkNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(LugbugNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ArtifactNpc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 24040, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);
            if (targetId == ScodaNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }
            if (targetId == MuninNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 1)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO2)
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                return false;
            }
            if (targetId == KasirNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 2)
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SETPRO3)
                    return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
                return false;
            }
            if (targetId == LyeanenerkNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 3)
                    return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                if (dialog == DialogAction.SETPRO4)
                    return await DefaultCloseDialogAsync(env, conn, 3, 4, ct);
                return false;
            }
            if (targetId == LugbugNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 4) return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                    if (var == 6) return await SendQuestDialogAsync(conn, targetObjId, 3057, ct);
                    return false;
                }
                if (dialog == DialogAction.SETPRO5)
                    return await DefaultCloseDialogAsync(env, conn, _itemDao, 4, 5, reward: false, sameNpc: false,
                        giveItemId: ArtifactItem, giveItemCount: 1, removeItemId: 0, removeItemCount: 0, ct);
                if (dialog == DialogAction.SET_SUCCEED)
                    return await DefaultCloseDialogAsync(env, conn, 6, 6, reward: true, sameNpc: false, ct);
                return false;
            }
            if (targetId == ArtifactNpc)
            {
                if (dialog == DialogAction.USE_OBJECT && var == 5 && player.Inventory.FindByItemId(ArtifactItem) is not null)
                    return await UseQuestObjectAsync(env, conn, 5, 6, reward: false, varNum: 0,
                        addItemId: 0, addItemCount: 0, removeItemId: ArtifactItem, removeItemCount: 1,
                        movieId: 291, dieObject: false, _itemDao, ct);
                return false;
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == ScodaNpc)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }
        return false;
    }
}
