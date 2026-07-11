// Port of Java data/scripts/system/handlers/quest/altgard/_2223AMythicalMonster.java
// (Mr. Poke/Gigi/vlog). Talk to Gefion (203616), talk to Lamir (203620), use the Old Incense
// Burner (700134, movie 67) to spawn a mythical monster (211621), kill it, turn in.
using System.Linq;
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

namespace Quest.Altgard;

public sealed class _2223AMythicalMonster : QuestHandlerBase
{
    private const int QuestIdConst  = 2223;
    private const int GefionNpc     = 203616;
    private const int LamirNpc      = 203620;
    private const int IncenseObj    = 700134;
    private const int MonsterNpc    = 211621;
    private const int IncenseItemId = 182203217;
    private const int MonsterMovie  = 67;
    private const int AltgardWorldId = 220030000;

    private readonly IItemDao _itemDao;

    public _2223AMythicalMonster(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(GefionNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(GefionNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(IncenseObj).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(LamirNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(MonsterNpc).OnKill.Add(QuestId);
        engine.RegisterOnQuestMovieEnd(MonsterMovie, QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId == GefionNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);
            switch (targetId)
            {
                case LamirNpc:
                    switch (dialog)
                    {
                        case DialogAction.QUEST_SELECT when var == 0:
                            return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                        case DialogAction.QUEST_SELECT when var == 1:
                            var held = player.Inventory.FindByItemId(IncenseItemId);
                            if ((held?.Count ?? 0) == 1)
                                return await SendQuestDialogAsync(conn, targetObjId, 1694, ct);
                            await GiveQuestItemAsync(player, conn, _itemDao, IncenseItemId, 1, ct);
                            return await SendQuestDialogAsync(conn, targetObjId, 1779, ct);
                        case DialogAction.SETPRO1:
                            return await DefaultCloseDialogAsync(env, conn, _itemDao, 0, 1, reward: false, sameNpc: false,
                                giveItemId: IncenseItemId, giveItemCount: 1, removeItemId: 0, removeItemCount: 0, ct);
                        case DialogAction.FINISH_DIALOG:
                            return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                        default:
                            return false;
                    }
                case IncenseObj:
                    if (dialog == DialogAction.USE_OBJECT)
                    {
                        var held = player.Inventory.FindByItemId(IncenseItemId);
                        if ((held?.Count ?? 0) == 1)
                            return await UseQuestObjectAsync(env, conn, step: 1, nextStep: 1, reward: false, varNum: 0,
                                addItemId: 0, addItemCount: 0, removeItemId: IncenseItemId, removeItemCount: 1,
                                movieId: MonsterMovie, dieObject: true, itemDao: _itemDao, ct: ct);
                    }
                    return false;
                case GefionNpc:
                    switch (dialog)
                    {
                        case DialogAction.QUEST_SELECT when var == 0:
                            return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                        case DialogAction.FINISH_DIALOG:
                            return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                        default:
                            return false;
                    }
                default:
                    return false;
            }
        }

        if (entry.Status == QuestStatus.REWARD && targetId == GefionNpc)
        {
            switch (dialog)
            {
                case DialogAction.USE_OBJECT:
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                case DialogAction.SELECT_QUEST_REWARD:
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                default:
                    return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }
        return false;
    }

    public override ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnKillEventAsync(env, conn, MonsterNpc, startVar: 1, reward: true, ct);

    public override ValueTask<bool> OnMovieEndAsync(QuestEnv env, int movieId, GsClientConnection conn, CancellationToken ct)
    {
        if (movieId != MonsterMovie) return ValueTask.FromResult(false);
        SpawnQuestNpc(AltgardWorldId, 1, MonsterNpc, 1547.1047f, 894.2969f, 248.019f, 85);
        return ValueTask.FromResult(true);
    }
}
