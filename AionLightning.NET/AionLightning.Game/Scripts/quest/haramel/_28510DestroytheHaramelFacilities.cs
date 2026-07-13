// Port of Java data/scripts/system/handlers/quest/haramel/_28510DestroytheHaramelFacilities.java.
// Asmodian mirror of _18510MurderMyShugo: accept from Gulkalla (203649), who hands over an item
// (182212021) on start; kill 3 Aether Carts (700950, vars 0->3), then use the Processed Odella
// object (700953) two more times (vars 3->5) before turning in back at Gulkalla.
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

namespace Quest.Haramel;

public sealed class _28510DestroytheHaramelFacilities : QuestHandlerBase
{
    private const int QuestIdConst   = 28510;
    private const int GulkallaNpc    = 203649;
    private const int ProcessedOdellaObj = 700953;
    private const int AetherCartNpc  = 700950;
    private const int StartItemId    = 182212021;

    private readonly IItemDao _itemDao;

    public _28510DestroytheHaramelFacilities(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(GulkallaNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(GulkallaNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ProcessedOdellaObj).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(AetherCartNpc).OnKill.Add(QuestId);
    }

    public override ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnKillEventAsync(env, conn, AetherCartNpc, 0, 3, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId == GulkallaNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);

                // Java: sendQuestStartDialog(env, 182212021, 1) — gives the item on QUEST_ACCEPT_1.
                if (env.DialogId == (int)DialogAction.QUEST_ACCEPT_1)
                {
                    if (await GiveQuestItemAsync(player, conn, _itemDao, StartItemId, 1, ct))
                        return await SendQuestStartDialogAsync(env, conn, ct);
                    return true;
                }
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START && targetId == ProcessedOdellaObj)
        {
            int var = entry.GetVar(0);
            if (dialog == DialogAction.USE_OBJECT)
            {
                if (var >= 3 && var < 5)
                {
                    await ChangeQuestStepAsync(conn, entry, 0, var + 1, toReward: false, ct);
                    return true;
                }
                if (var == 5)
                {
                    await ChangeQuestStepAsync(conn, entry, 0, 5, toReward: true, ct);
                    return true;
                }
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == GulkallaNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            if (dialog == DialogAction.SELECT_QUEST_REWARD)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }
}
