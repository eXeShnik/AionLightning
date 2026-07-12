// Port of Java data/scripts/system/handlers/quest/heiron/_1058AetherInsanity.java.
// Talk to Mabangtah (204020, var0->1), then Sarantus (204501): var1->2, collect-check flips
// straight to REWARD and turns in at the same NPC. Mission-chain quest (no NPC quest-offer
// dialog), gated on 1500. Skip vs Java: the teleport-to-210040000 on SETPRO1 isn't ported — no
// teleport service exists in this port yet; the var transition still applies.
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

namespace Quest.Heiron;

public sealed class _1058AetherInsanity : QuestHandlerBase
{
    private const int QuestIdConst = 1058;
    private const int MabangtahNpc = 204020;
    private const int SarantusNpc  = 204501;

    private readonly IItemDao _itemDao;

    public _1058AetherInsanity(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(MabangtahNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(SarantusNpc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, 1500, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == SarantusNpc) return await SendQuestEndDialogAsync(env, conn, ct);
            return false;
        }
        if (entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);
        if (targetId == MabangtahNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 0)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog == DialogAction.SETPRO1 && var == 0)
                return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
        }
        else if (targetId == SarantusNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT)
            {
                if (var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (var == 2) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                return false;
            }
            if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM && var == 2)
                return await CheckQuestItemsAsync(env, conn, _itemDao, 2, 2, true, 5, 10001, ct);
            if (dialog == DialogAction.SELECT_ACTION_1353)
            {
                await PlayQuestMovieAsync(conn, player, 191, ct);
                return false;
            }
            if (dialog == DialogAction.SETPRO2 && var == 1)
                return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
        }
        return false;
    }
}
