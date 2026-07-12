// Port of Java data/scripts/system/handlers/quest/reshanta/_2074LookingforLeibo.java (Hellboy aion4Free/Gigi/vlog).
// Talk to Scoda (278036, var 0->1), Munin (203550, var 1->2), Kasir (204207, var 2->3), Lyeanenerk
// (798067, var 3->4), Lugbug (279029, var 4->5, gives item 188020000), use the Artefact of the
// Inception (700355, var 5->6, consumes the item, movie 291), then back to Lugbug (var 6 ->
// REWARD). Zone-mission chain, level-up gated on 2701.
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

public sealed class _2074LookingforLeibo : QuestHandlerBase
{
    private const int QuestIdConst   = 2074;
    private const int ScodaNpc       = 278036;
    private const int MuninNpc       = 203550;
    private const int KasirNpc       = 204207;
    private const int LyeanenerkNpc  = 798067;
    private const int LugbugNpc      = 279029;
    private const int ArtefactObj    = 700355;
    private const int TokenItem      = 188020000;

    private readonly IItemDao _itemDao;

    public _2074LookingforLeibo(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        foreach (int npc in new[] { ScodaNpc, MuninNpc, KasirNpc, LyeanenerkNpc, LugbugNpc, ArtefactObj })
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 2701, isZoneMission: true, ct);

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
            if (targetId != ScodaNpc) return false;
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        if (entry.Status != QuestStatus.START) return false;

        switch (targetId)
        {
            case ScodaNpc:
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT when var == 0:
                        return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                    case DialogAction.SETPRO1 when var == 0:
                        return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                    default:
                        return false;
                }
            case MuninNpc:
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT when var == 1:
                        return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    case DialogAction.SETPRO2 when var == 1:
                        return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                    default:
                        return false;
                }
            case KasirNpc:
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT when var == 2:
                        return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                    case DialogAction.SETPRO3 when var == 2:
                        return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
                    default:
                        return false;
                }
            case LyeanenerkNpc:
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT when var == 3:
                        return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                    case DialogAction.SETPRO4 when var == 3:
                        return await DefaultCloseDialogAsync(env, conn, 3, 4, ct);
                    default:
                        return false;
                }
            case LugbugNpc:
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT when var == 4:
                        return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                    case DialogAction.QUEST_SELECT when var == 6:
                        return await SendQuestDialogAsync(conn, targetObjId, 3057, ct);
                    case DialogAction.SETPRO5 when var == 4:
                        return await DefaultCloseDialogAsync(env, conn, _itemDao, 4, 5, reward: false, sameNpc: false,
                            giveItemId: TokenItem, giveItemCount: 1, removeItemId: 0, removeItemCount: 0, ct);
                    case DialogAction.SET_SUCCEED when var == 6:
                        return await DefaultCloseDialogAsync(env, conn, 6, 6, reward: true, sameNpc: false, ct);
                    default:
                        return false;
                }
            case ArtefactObj:
                if (dialog != DialogAction.USE_OBJECT || var != 5) return false;
                if ((player.Inventory.FindByItemId(TokenItem)?.Count ?? 0) <= 0) return false;
                return await UseQuestObjectAsync(env, conn, step: 5, nextStep: 6, reward: false, varNum: 0,
                    addItemId: 0, addItemCount: 0, removeItemId: TokenItem, removeItemCount: 1,
                    movieId: 291, dieObject: false, _itemDao, ct);
            default:
                return false;
        }
    }
}
